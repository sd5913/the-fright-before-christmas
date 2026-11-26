using System;
using System.Collections.Generic;
using UnityEngine;

public class LootItem : MonoBehaviour
{
    public static IReadOnlyCollection<LootItem> ActiveItems => s_activeItems;
    private static readonly HashSet<LootItem> s_activeItems = new HashSet<LootItem>();

    [SerializeField] private float defaultVacuumLerpSpeed = 14f;
    [SerializeField] private float defaultRotationLerpSpeed = 16f;
    [SerializeField] private float defaultShrinkLerpSpeed = 10f;
    [SerializeField] private float defaultMaxCollectDuration = 0.5f;
    [SerializeField] private float collectCompleteDistance = 0.06f;
    [SerializeField] private AudioPoolSound onCollectedSound;

    private Transform _collectorTarget;
    private bool _isBeingCollected;
    private float _vacuumLerpSpeed;
    private float _rotationLerpSpeed;
    private float _shrinkLerpSpeed;
    private float _maxCollectDuration;
    private float _collectElapsed;
    private bool _collectionResolved;

    public static Action OnLootCollected;

    private void OnEnable()
    {
        s_activeItems.Add(this);
    }

    private void OnDisable()
    {
        s_activeItems.Remove(this);
    }

    private void Update()
    {
        if (!_isBeingCollected || _collectorTarget == null)
        {
            return;
        }

        if (_collectionResolved)
        {
            return;
        }

        _collectElapsed += Time.deltaTime;
        var targetPosition = _collectorTarget.position;
        var positionT = 1f - Mathf.Exp(-_vacuumLerpSpeed * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, targetPosition, positionT);

        var toTarget = targetPosition - transform.position;
        if (toTarget.sqrMagnitude > 0.000001f)
        {
            var targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
            var rotationT = 1f - Mathf.Exp(-_rotationLerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationT);
        }

        var scaleT = 1f - Mathf.Exp(-_shrinkLerpSpeed * Time.deltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, scaleT);

        if (Vector3.Distance(transform.position, targetPosition) <= collectCompleteDistance
            || _collectElapsed >= _maxCollectDuration || transform.localScale == Vector3.zero)
        {
            _collectionResolved = true;
            if (onCollectedSound != null && AudioPool.Instance != null)
            {
                AudioPool.Instance.play(onCollectedSound);
            }

            OnLootCollected?.Invoke();
            Destroy(gameObject);
        }
    }

    public bool BeginCollect(
        Transform collectorTarget,
        float vacuumLerpSpeed,
        float rotationLerpSpeed,
        float shrinkLerpSpeed,
        float maxCollectDuration)
    {
        if (_isBeingCollected || collectorTarget == null)
        {
            return false;
        }

        _isBeingCollected = true;
        _collectorTarget = collectorTarget;
        _vacuumLerpSpeed = Mathf.Max(0.01f, vacuumLerpSpeed > 0f ? vacuumLerpSpeed : defaultVacuumLerpSpeed);
        _rotationLerpSpeed = Mathf.Max(0.01f, rotationLerpSpeed > 0f ? rotationLerpSpeed : defaultRotationLerpSpeed);
        _shrinkLerpSpeed = Mathf.Max(0.01f, shrinkLerpSpeed > 0f ? shrinkLerpSpeed : defaultShrinkLerpSpeed);
        _maxCollectDuration = Mathf.Max(0.05f, maxCollectDuration > 0f ? maxCollectDuration : defaultMaxCollectDuration);
        _collectElapsed = 0f;
        _collectionResolved = false;
        SetPhysicsActive(false);
        return true;
    }

    private void SetPhysicsActive(bool active)
    {
        var colliders = GetComponentsInChildren<Collider>(true);
        for (var i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = active;
        }

        var rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            return;
        }

        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        rigidbody.isKinematic = !active;
        rigidbody.useGravity = active;
    }

    private void OnValidate()
    {
        defaultVacuumLerpSpeed = Mathf.Max(0.01f, defaultVacuumLerpSpeed);
        defaultRotationLerpSpeed = Mathf.Max(0.01f, defaultRotationLerpSpeed);
        defaultShrinkLerpSpeed = Mathf.Max(0.01f, defaultShrinkLerpSpeed);
        defaultMaxCollectDuration = Mathf.Max(0.05f, defaultMaxCollectDuration);
        collectCompleteDistance = Mathf.Max(0.005f, collectCompleteDistance);
    }
}
