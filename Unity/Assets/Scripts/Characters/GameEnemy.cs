using UnityEngine;

public class GameEnemy : MonoBehaviour
{
    [SerializeField] private Freezable freezable;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private bool useInitialZPosition = true;
    [SerializeField] private float fixedZPosition;
    [SerializeField] private ProtectionArea protectionArea;
    [SerializeField] private float randomTargetRefreshInterval = 1.5f;
    [SerializeField] private float randomTargetReachDistance = 0.2f;
    [SerializeField] private bool rotateTowardMovement = true;
    [SerializeField] private float rotationLerpSpeed = 12f;
    [SerializeField] private float gameOverEscapeSpeedMultiplier = 1.35f;
    [SerializeField] private float gameOverEscapeViewportPadding = 0.2f;

    private BoxCollider _protectionAreaCollider;
    private Vector2 _currentTargetXY;
    private float _nextRandomTargetTime;
    private bool _hasGameOverEscapeTarget;
    private Vector2 _gameOverEscapeTargetXY;

    private void Awake()
    {
        if (useInitialZPosition)
        {
            fixedZPosition = transform.position.z;
        }

        RefreshProtectionAreaReferences();
        _currentTargetXY = new Vector2(transform.position.x, transform.position.y);
    }

    private void Update()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.GameOver)
        {
            UpdateGameOverEscape();
            return;
        }

        if (freezable != null && freezable.IsFreezed)
        {
            KeepFixedZ();
            return;
        }

        RefreshProtectionAreaReferences();
        if (_protectionAreaCollider == null)
        {
            KeepFixedZ();
            return;
        }

        var current = transform.position;
        var currentXY = new Vector2(current.x, current.y);
        var shouldPickNewTarget = Time.time >= _nextRandomTargetTime
            || Vector2.Distance(currentXY, _currentTargetXY) <= randomTargetReachDistance;
        if (shouldPickNewTarget)
        {
            _currentTargetXY = GetRandomPointInProtectionArea();
            _nextRandomTargetTime = Time.time + Mathf.Max(0.01f, randomTargetRefreshInterval);
        }

        var movedXY = Vector2.MoveTowards(
            currentXY,
            _currentTargetXY,
            moveSpeed * Time.deltaTime);

        ApplyMovementRotation(currentXY, movedXY);
        transform.position = new Vector3(movedXY.x, movedXY.y, fixedZPosition);
    }

    private void UpdateGameOverEscape()
    {
        if (!_hasGameOverEscapeTarget)
        {
            _hasGameOverEscapeTarget = TryGetRandomScreenEdgeTarget(out _gameOverEscapeTargetXY);
            if (!_hasGameOverEscapeTarget)
            {
                var randomDirection = Random.insideUnitCircle.normalized;
                if (randomDirection.sqrMagnitude <= 0.0001f)
                {
                    randomDirection = Vector2.up;
                }

                var current = transform.position;
                _gameOverEscapeTargetXY = new Vector2(current.x, current.y) + randomDirection * 20f;
                _hasGameOverEscapeTarget = true;
            }
        }

        var currentPosition = transform.position;
        var currentXY = new Vector2(currentPosition.x, currentPosition.y);
        var movedXY = Vector2.MoveTowards(
            currentXY,
            _gameOverEscapeTargetXY,
            moveSpeed * Mathf.Max(1f, gameOverEscapeSpeedMultiplier) * Time.deltaTime);

        ApplyMovementRotation(currentXY, movedXY);
        transform.position = new Vector3(movedXY.x, movedXY.y, fixedZPosition);
    }

    private bool TryGetRandomScreenEdgeTarget(out Vector2 targetXY)
    {
        var activeCamera = Camera.main;
        if (activeCamera == null)
        {
            targetXY = Vector2.zero;
            return false;
        }

        var side = Random.Range(0, 4);
        float viewportX;
        float viewportY;

        switch (side)
        {
            case 0:
                viewportX = -gameOverEscapeViewportPadding;
                viewportY = Random.value;
                break;
            case 1:
                viewportX = 1f + gameOverEscapeViewportPadding;
                viewportY = Random.value;
                break;
            case 2:
                viewportX = Random.value;
                viewportY = -gameOverEscapeViewportPadding;
                break;
            default:
                viewportX = Random.value;
                viewportY = 1f + gameOverEscapeViewportPadding;
                break;
        }

        var ray = activeCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));
        var movementPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, fixedZPosition));
        if (!movementPlane.Raycast(ray, out var enter))
        {
            targetXY = Vector2.zero;
            return false;
        }

        var worldPoint = ray.GetPoint(enter);
        targetXY = new Vector2(worldPoint.x, worldPoint.y);
        return true;
    }

    private void ApplyMovementRotation(Vector2 currentXY, Vector2 movedXY)
    {
        var moveDirection = movedXY - currentXY;
        if (rotateTowardMovement && moveDirection.sqrMagnitude > 0.000001f)
        {
            // Enemy eyes point toward local -forward, so invert direction here.
            var desiredRotation = Quaternion.LookRotation(
                new Vector3(-moveDirection.x, -moveDirection.y, 0f),
                Vector3.forward);

            var nextRotation = rotationLerpSpeed <= 0f
                ? desiredRotation
                : Quaternion.Slerp(transform.rotation, desiredRotation, 1f - Mathf.Exp(-rotationLerpSpeed * Time.deltaTime));

            var euler = nextRotation.eulerAngles;
            euler.z = 0f;
            transform.rotation = Quaternion.Euler(euler);
        }
    }

    private void RefreshProtectionAreaReferences()
    {
        if (protectionArea == null)
        {
            protectionArea = FindAnyObjectByType<ProtectionArea>();
        }

        if (protectionArea == null)
        {
            _protectionAreaCollider = null;
            return;
        }

        if (_protectionAreaCollider == null || _protectionAreaCollider.gameObject != protectionArea.gameObject)
        {
            _protectionAreaCollider = protectionArea.GetComponent<BoxCollider>();
        }
    }

    private Vector2 GetRandomPointInProtectionArea()
    {
        var center = _protectionAreaCollider.center;
        var size = _protectionAreaCollider.size * 0.5f;

        var localPoint = new Vector3(
            center.x + Random.Range(-size.x, size.x),
            center.y + Random.Range(-size.y, size.y),
            center.z + Random.Range(-size.z, size.z));

        var worldPoint = _protectionAreaCollider.transform.TransformPoint(localPoint);
        return new Vector2(worldPoint.x, worldPoint.y);
    }

    private void KeepFixedZ()
    {
        var position = transform.position;
        if (Mathf.Approximately(position.z, fixedZPosition))
        {
            return;
        }

        position.z = fixedZPosition;
        transform.position = position;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        randomTargetRefreshInterval = Mathf.Max(0.01f, randomTargetRefreshInterval);
        randomTargetReachDistance = Mathf.Max(0.01f, randomTargetReachDistance);
        rotationLerpSpeed = Mathf.Max(0f, rotationLerpSpeed);
        gameOverEscapeSpeedMultiplier = Mathf.Max(0f, gameOverEscapeSpeedMultiplier);
        gameOverEscapeViewportPadding = Mathf.Max(0f, gameOverEscapeViewportPadding);
    }
}