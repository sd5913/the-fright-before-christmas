using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class FakeHandShooter : CharacterBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handOrigin;
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform throwUpReference;

    [Header("Throw")]
    [SerializeField] private float throwSpeed = 8.5f;
    [SerializeField] private float upwardBoost = 1.4f;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float spawnForwardOffset = 0.06f;

    [Header("Distance Scaling")]
    [SerializeField] private bool scaleThrowByDistance = true;
    [SerializeField] private float distanceForMaxThrow = 6f;
    [SerializeField] private float minSpeedMultiplier = 0.3f;
    [SerializeField] private float minUpwardMultiplier = 0.45f;

    [Header("Held Projectile")]
    [SerializeField] private bool disableHeldColliders = true;

    [Header("Spawn Punch")]
    [SerializeField] private bool punchOnSpawn = true;
    [SerializeField] private float punchAmount = 0.2f;
    [SerializeField] private float punchDuration = 0.2f;
    [SerializeField] private int punchVibrato = 2;

    [Header("Shoot Feedback")]
    [SerializeField] private float shootShakeIntensity = 1f;

    private float _nextFireTime;
    private GameObject _heldProjectile;
    private Coroutine _punchRoutine;
    private Coroutine _spawnHeldRoutine;

    private void Start()
    {
        SpawnHeldProjectile();
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (handOrigin == null || projectilePrefab == null)
        {
            return;
        }

        if (_heldProjectile == null && _spawnHeldRoutine == null)
        {
            SpawnHeldProjectile();
        }

        if (Time.time < _nextFireTime || !MouseInputManager.GetLeftPressedThisFrame())
        {
            return;
        }

        var activeCamera = sourceCamera != null ? sourceCamera : Camera.main;
        if (activeCamera == null || !MouseInputManager.TryGetMousePosition(out var mousePosition))
        {
            return;
        }

        var upAxis = throwUpReference != null ? throwUpReference.up : transform.up;
        var launchPosition = _heldProjectile != null ? _heldProjectile.transform.position : handOrigin.position;
        var handScreen = activeCamera.WorldToScreenPoint(launchPosition);
        if (handScreen.z <= 0f)
        {
            return;
        }

        var targetWorld = activeCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, handScreen.z));
        var direction = (targetWorld - launchPosition).normalized;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = handOrigin.forward;
        }

        var speed = throwSpeed;
        var verticalBoost = upwardBoost;
        if (scaleThrowByDistance)
        {
            var distance = Vector3.Distance(launchPosition, targetWorld);
            var t = Mathf.Clamp01(distance / Mathf.Max(0.01f, distanceForMaxThrow));
            t = t * t * (3f - 2f * t); // smoothstep
            speed *= Mathf.Lerp(minSpeedMultiplier, 1f, t);
            verticalBoost *= Mathf.Lerp(minUpwardMultiplier, 1f, t);
        }

        var initialVelocity = direction * speed + upAxis * verticalBoost;
        var rotation = initialVelocity.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(initialVelocity.normalized, upAxis)
            : handOrigin.rotation;

        var firedProjectile = _heldProjectile;
        _heldProjectile = null;
        if (firedProjectile == null)
        {
            SpawnHeldProjectile();
            return;
        }

        if (_punchRoutine != null)
        {
            StopCoroutine(_punchRoutine);
            _punchRoutine = null;
        }

        firedProjectile.transform.SetParent(null, true);
        firedProjectile.transform.rotation = rotation;
        firedProjectile.transform.localScale = projectilePrefab.transform.localScale;

        if (disableHeldColliders)
        {
            SetCollidersEnabled(firedProjectile, true);
        }

        if (firedProjectile.TryGetComponent<AutoDestroy>(out var autoDestroyOnFired))
        {
            autoDestroyOnFired.SetPaused(false);
        }

        if (firedProjectile.TryGetComponent<ThrownProjectile>(out var thrownProjectile))
        {
            thrownProjectile.Launch(initialVelocity);
        }
        else if (firedProjectile.TryGetComponent<Rigidbody>(out var rigidbody))
        {
            rigidbody.isKinematic = false;
            rigidbody.useGravity = true;
            rigidbody.velocity = initialVelocity;
        }

        if (CameraShakeController.Instance != null)
        {
            CameraShakeController.Instance.PlayDefaultShake(shootShakeIntensity);
        }

        _nextFireTime = Time.time + fireRate;
        QueueSpawnHeldProjectile(fireRate);
    }

    private void OnValidate()
    {
        throwSpeed = Mathf.Max(0.1f, throwSpeed);
        fireRate = Mathf.Max(0f, fireRate);
        distanceForMaxThrow = Mathf.Max(0.01f, distanceForMaxThrow);
        minSpeedMultiplier = Mathf.Clamp01(minSpeedMultiplier);
        minUpwardMultiplier = Mathf.Clamp01(minUpwardMultiplier);
        punchAmount = Mathf.Max(0f, punchAmount);
        punchDuration = Mathf.Max(0.01f, punchDuration);
        punchVibrato = Mathf.Max(1, punchVibrato);
        shootShakeIntensity = Mathf.Max(0f, shootShakeIntensity);
    }

    private void OnDisable()
    {
        if (_spawnHeldRoutine != null)
        {
            StopCoroutine(_spawnHeldRoutine);
            _spawnHeldRoutine = null;
        }

        if (_heldProjectile != null)
        {
            Destroy(_heldProjectile);
            _heldProjectile = null;
        }
    }

    private void SpawnHeldProjectile()
    {
        if (handOrigin == null || projectilePrefab == null || _heldProjectile != null)
        {
            return;
        }

        var spawnPosition = handOrigin.position + handOrigin.forward * spawnForwardOffset;
        _heldProjectile = Instantiate(projectilePrefab, spawnPosition, handOrigin.rotation);
        _heldProjectile.transform.SetParent(handOrigin, true);
        ApplyHeldWorldScaleFromPrefab();

        if (_heldProjectile.TryGetComponent<ThrownProjectile>(out var thrownProjectile))
        {
            thrownProjectile.SetHeldState(true);
        }
        else if (_heldProjectile.TryGetComponent<Rigidbody>(out var rigidbody))
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.useGravity = false;
            rigidbody.isKinematic = true;
        }

        if (disableHeldColliders)
        {
            SetCollidersEnabled(_heldProjectile, false);
        }

        if (_heldProjectile.TryGetComponent<AutoDestroy>(out var autoDestroyOnHeld))
        {
            autoDestroyOnHeld.SetPaused(true);
        }

        if (punchOnSpawn)
        {
            if (_punchRoutine != null)
            {
                StopCoroutine(_punchRoutine);
            }

            _punchRoutine = StartCoroutine(PunchScaleRoutine(_heldProjectile.transform));
        }
    }

    private void QueueSpawnHeldProjectile(float delay)
    {
        if (_spawnHeldRoutine != null)
        {
            StopCoroutine(_spawnHeldRoutine);
        }

        _spawnHeldRoutine = StartCoroutine(SpawnHeldAfterDelay(delay));
    }

    private IEnumerator SpawnHeldAfterDelay(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        SpawnHeldProjectile();
        _spawnHeldRoutine = null;
    }

    private void ApplyHeldWorldScaleFromPrefab()
    {
        if (_heldProjectile == null || projectilePrefab == null)
        {
            return;
        }

        SetWorldScale(_heldProjectile.transform, projectilePrefab.transform.localScale);
    }

    private static void SetWorldScale(Transform target, Vector3 worldScale)
    {
        var parentScale = target.parent != null ? target.parent.lossyScale : Vector3.one;
        target.localScale = new Vector3(
            SafeDivide(worldScale.x, parentScale.x),
            SafeDivide(worldScale.y, parentScale.y),
            SafeDivide(worldScale.z, parentScale.z));
    }

    private static float SafeDivide(float value, float divisor)
    {
        return Mathf.Abs(divisor) > 0.0001f ? value / divisor : 0f;
    }

    private static void SetCollidersEnabled(GameObject root, bool enabled)
    {
        var colliders = root.GetComponentsInChildren<Collider>(true);
        for (var i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = enabled;
        }
    }

    private IEnumerator PunchScaleRoutine(Transform target)
    {
        if (target == null)
        {
            yield break;
        }

        var baseScale = target.localScale;
        var elapsed = 0f;

        while (elapsed < punchDuration && target != null)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / punchDuration);
            var wave = Mathf.Sin(t * Mathf.PI * punchVibrato);
            var decay = 1f - t;
            var multiplier = 1f + wave * decay * punchAmount;
            target.localScale = baseScale * multiplier;
            yield return null;
        }

        if (target != null)
        {
            target.localScale = baseScale;
        }

        _punchRoutine = null;
    }
}
