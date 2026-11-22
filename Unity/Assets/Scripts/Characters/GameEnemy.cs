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

    private BoxCollider _protectionAreaCollider;
    private Vector2 _currentTargetXY;
    private float _nextRandomTargetTime;

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

        transform.position = new Vector3(movedXY.x, movedXY.y, fixedZPosition);
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
    }
}