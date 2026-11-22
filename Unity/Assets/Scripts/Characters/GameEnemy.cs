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
    }
}