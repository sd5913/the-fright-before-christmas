using UnityEngine;

[DisallowMultipleComponent]
public class FakeHandShooter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handOrigin;
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform throwUpReference;

    [Header("Throw")]
    [SerializeField] private float throwSpeed = 8.5f;
    [SerializeField] private float upwardBoost = 1.4f;
    [SerializeField] private float fireCooldown = 0.2f;
    [SerializeField] private float spawnForwardOffset = 0.06f;

    private float _nextFireTime;

    private void Update()
    {
        if (Time.time < _nextFireTime)
        {
            return;
        }

        if (!MouseInputManager.GetLeftPressedThisFrame())
        {
            return;
        }

        if (handOrigin == null || projectilePrefab == null)
        {
            return;
        }

        var activeCamera = sourceCamera != null ? sourceCamera : Camera.main;
        if (activeCamera == null || !MouseInputManager.TryGetMousePosition(out var mousePosition))
        {
            return;
        }

        var upAxis = throwUpReference != null ? throwUpReference.up : transform.up;
        var spawnPosition = handOrigin.position + handOrigin.forward * spawnForwardOffset;
        var handScreen = activeCamera.WorldToScreenPoint(spawnPosition);
        if (handScreen.z <= 0f)
        {
            return;
        }

        var targetWorld = activeCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, handScreen.z));
        var direction = (targetWorld - spawnPosition).normalized;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = handOrigin.forward;
        }

        var initialVelocity = direction * throwSpeed + upAxis * upwardBoost;
        var rotation = initialVelocity.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(initialVelocity.normalized, upAxis)
            : handOrigin.rotation;

        var instance = Instantiate(projectilePrefab, spawnPosition, rotation);
        if (instance.TryGetComponent<ThrownProjectile>(out var thrownProjectile))
        {
            thrownProjectile.Launch(initialVelocity);
        }
        else if (instance.TryGetComponent<Rigidbody>(out var rigidbody))
        {
            rigidbody.velocity = initialVelocity;
        }

        _nextFireTime = Time.time + fireCooldown;
    }

    private void OnValidate()
    {
        throwSpeed = Mathf.Max(0.1f, throwSpeed);
        fireCooldown = Mathf.Max(0f, fireCooldown);
    }
}
