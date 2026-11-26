using UnityEngine;

public class LootCollector : CharacterBehaviour
{
    [SerializeField] private float collectRange = 3f;
    [SerializeField] private float collectConeAngle = 45f;
    [SerializeField] private float vacuumLerpSpeed = 14f;
    [SerializeField] private float rotationLerpSpeed = 16f;
    [SerializeField] private float shrinkLerpSpeed = 10f;
    [SerializeField] private float maxCollectDuration = 0.5f;
    [SerializeField] private bool onlyWhenFocused = true;
    [Header("Vacuum Audio")]
    [SerializeField] private AudioClip vacuumLoopClip;
    [SerializeField] private AudioSource vacuumLoopSource;
    [SerializeField] private float vacuumLoopVolume = 0.4f;
    [SerializeField] private float vacuumLoopFadeInSpeed = 4f;
    [SerializeField] private float vacuumLoopFadeOutSpeed = 6f;
    [SerializeField, Range(0f, 1f)] private float vacuumLoopSpatialBlend = 0f;

    private readonly System.Collections.Generic.List<LootItem> _lootBuffer = new System.Collections.Generic.List<LootItem>(64);

    private void Awake()
    {
        if (vacuumLoopClip != null)
        {
            EnsureVacuumAudioSource();
        }
    }

    protected override void OnUpdate(float deltaTime)
    {
        UpdateVacuumLoop(deltaTime);

        if (onlyWhenFocused && (gameCharacter == null || !gameCharacter.IsFocused))
        {
            return;
        }

        var forwardXY = new Vector2(transform.forward.x, transform.forward.y).normalized;
        if (forwardXY.sqrMagnitude <= 0.000001f)
        {
            forwardXY = Vector2.up;
        }

        var originXY = new Vector2(transform.position.x, transform.position.y);
        var halfAngleRadians = collectConeAngle * 0.5f * Mathf.Deg2Rad;
        var coneRadiusScale = Mathf.Tan(halfAngleRadians);

        _lootBuffer.Clear();
        foreach (var lootItem in LootItem.ActiveItems)
        {
            if (lootItem != null)
            {
                _lootBuffer.Add(lootItem);
            }
        }

        for (var i = 0; i < _lootBuffer.Count; i++)
        {
            var lootItem = _lootBuffer[i];
            var lootPos = lootItem.transform.position;
            var toLootXY = new Vector2(lootPos.x, lootPos.y) - originXY;
            var axialDistance = Vector2.Dot(forwardXY, toLootXY);
            if (axialDistance < 0f || axialDistance > collectRange)
            {
                continue;
            }

            if (axialDistance <= Mathf.Epsilon)
            {
                lootItem.BeginCollect(transform, vacuumLerpSpeed, rotationLerpSpeed, shrinkLerpSpeed, maxCollectDuration);
                continue;
            }

            var radialVector = toLootXY - forwardXY * axialDistance;
            var radialDistance = radialVector.magnitude;
            var maxRadiusAtDistance = axialDistance * coneRadiusScale;
            if (radialDistance > maxRadiusAtDistance)
            {
                continue;
            }

            lootItem.BeginCollect(transform, vacuumLerpSpeed, rotationLerpSpeed, shrinkLerpSpeed, maxCollectDuration);
        }
    }

    private void OnDisable()
    {
        if (vacuumLoopSource == null)
        {
            return;
        }

        vacuumLoopSource.Stop();
        vacuumLoopSource.volume = 0f;
    }

    private void OnValidate()
    {
        collectRange = Mathf.Max(0.01f, collectRange);
        collectConeAngle = Mathf.Clamp(collectConeAngle, 1f, 179f);
        vacuumLerpSpeed = Mathf.Max(0.01f, vacuumLerpSpeed);
        rotationLerpSpeed = Mathf.Max(0.01f, rotationLerpSpeed);
        shrinkLerpSpeed = Mathf.Max(0.01f, shrinkLerpSpeed);
        maxCollectDuration = Mathf.Max(0.05f, maxCollectDuration);
        vacuumLoopVolume = Mathf.Clamp01(vacuumLoopVolume);
        vacuumLoopFadeInSpeed = Mathf.Max(0.01f, vacuumLoopFadeInSpeed);
        vacuumLoopFadeOutSpeed = Mathf.Max(0.01f, vacuumLoopFadeOutSpeed);
        vacuumLoopSpatialBlend = Mathf.Clamp01(vacuumLoopSpatialBlend);

        if (vacuumLoopSource != null)
        {
            ConfigureVacuumAudioSource(vacuumLoopSource);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, collectRange);

        var half = collectConeAngle * 0.5f;
        var left = Quaternion.AngleAxis(-half, transform.up) * transform.forward;
        var right = Quaternion.AngleAxis(half, transform.up) * transform.forward;
        Gizmos.DrawRay(transform.position, left * collectRange);
        Gizmos.DrawRay(transform.position, right * collectRange);
    }

    private void UpdateVacuumLoop(float deltaTime)
    {
        if (vacuumLoopClip == null)
        {
            if (vacuumLoopSource != null && vacuumLoopSource.isPlaying)
            {
                vacuumLoopSource.Stop();
                vacuumLoopSource.volume = 0f;
            }

            return;
        }

        var isFocused = gameCharacter != null && gameCharacter.IsFocused;
        var targetVolume = isFocused ? vacuumLoopVolume : 0f;

        EnsureVacuumAudioSource();
        if (vacuumLoopSource == null)
        {
            return;
        }

        if (vacuumLoopSource.clip != vacuumLoopClip)
        {
            vacuumLoopSource.clip = vacuumLoopClip;
        }

        if (isFocused && !vacuumLoopSource.isPlaying)
        {
            vacuumLoopSource.volume = 0f;
            vacuumLoopSource.Play();
        }

        var fadeSpeed = targetVolume > vacuumLoopSource.volume ? vacuumLoopFadeInSpeed : vacuumLoopFadeOutSpeed;
        vacuumLoopSource.volume = Mathf.MoveTowards(vacuumLoopSource.volume, targetVolume, fadeSpeed * deltaTime);

        if (!isFocused && vacuumLoopSource.isPlaying && vacuumLoopSource.volume <= 0.0001f)
        {
            vacuumLoopSource.Stop();
        }
    }

    private void EnsureVacuumAudioSource()
    {
        if (vacuumLoopSource == null)
        {
            vacuumLoopSource = gameObject.AddComponent<AudioSource>();
        }

        ConfigureVacuumAudioSource(vacuumLoopSource);
    }

    private void ConfigureVacuumAudioSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = true;
        source.volume = Mathf.Clamp01(source.volume);
        source.clip = vacuumLoopClip;
        source.spatialBlend = vacuumLoopSpatialBlend;
    }
}
