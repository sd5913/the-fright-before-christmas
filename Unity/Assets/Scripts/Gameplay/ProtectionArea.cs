using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class ProtectionArea : MonoBehaviour
{
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    public float CurrentHealth => currentHealth;

    [Header("Area Health")]
    [SerializeField] private float maxHealth = 20f;
    [SerializeField] private float currentHealth = 20f;
    [SerializeField] private float damagePerEnemyPerTick = 1f;
    [SerializeField] private float damageInterval = 1f;

    [Header("Hit Feedback")]
    [SerializeField] private float hitShakeIntensity = 1f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private Renderer[] damageFlashRenderers;
    
    [Header("Audio")]
    [SerializeField] private AudioPoolSound onDamageSound;

    private readonly HashSet<GameEnemy> _enemiesInside = new HashSet<GameEnemy>();
    private float _nextDamageTime;

    private Coroutine _damageFlashRoutine;
    private MaterialPropertyBlock[] _damageFlashBlocks;
    private Color[] _damageFlashBaseColors;
    private bool[] _damageFlashUsesBaseColor;

    private void Awake()
    {
        maxHealth = Mathf.Max(0f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        EnsureDamageFlashTargets();
        _nextDamageTime = Time.time + damageInterval;
    }

    private void Update()
    {
        if (currentHealth <= 0f)
        {
            return;
        }

        CleanupInvalidEnemies();
        if (_enemiesInside.Count == 0 || Time.time < _nextDamageTime)
        {
            return;
        }

        var activeDamagingEnemies = CountDamagingEnemies();
        if (activeDamagingEnemies <= 0)
        {
            return;
        }

        var damage = damagePerEnemyPerTick * activeDamagingEnemies;
        ApplyDamage(damage);
        _nextDamageTime = Time.time + damageInterval;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || currentHealth <= 0f)
        {
            return;
        }

        var enemy = other.GetComponent<GameEnemy>() ?? other.GetComponentInParent<GameEnemy>();
        if (enemy == null || enemy.gameObject == gameObject)
        {
            return;
        }

        var hadNoEnemies = _enemiesInside.Count == 0;
        _enemiesInside.Add(enemy);
        if (hadNoEnemies && _enemiesInside.Count > 0)
        {
            _nextDamageTime = Time.time + damageInterval;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null)
        {
            return;
        }

        var enemy = other.GetComponent<GameEnemy>() ?? other.GetComponentInParent<GameEnemy>();
        if (enemy == null)
        {
            return;
        }

        _enemiesInside.Remove(enemy);
    }

    public void ApplyDamage(float damage)
    {
        if (damage <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        TriggerDamageFlash();

        if (onDamageSound != null && AudioPool.Instance != null)
        {
            AudioPool.Instance.play(onDamageSound);
        }

        if (CameraFeedback.Instance != null && hitShakeIntensity > 0f)
        {
            CameraFeedback.Instance.PlayDefaultShake(hitShakeIntensity);
        }

        if (currentHealth <= 0f)
        {
            if (LevelManager.Instance != null)
            {
                LevelManager.Instance.SetGameOver(true);
            }

            Destroy(gameObject);
        }
    }

    private void CleanupInvalidEnemies()
    {
        if (_enemiesInside.Count == 0)
        {
            return;
        }

        _tempEnemiesToRemove.Clear();
        foreach (var enemy in _enemiesInside)
        {
            if (enemy == null)
            {
                _tempEnemiesToRemove.Add(enemy);
                continue;
            }

            var enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null && enemyHealth.CurrentHealth <= 0f)
            {
                _tempEnemiesToRemove.Add(enemy);
            }
        }

        for (var i = 0; i < _tempEnemiesToRemove.Count; i++)
        {
            _enemiesInside.Remove(_tempEnemiesToRemove[i]);
        }
    }

    private readonly List<GameEnemy> _tempEnemiesToRemove = new List<GameEnemy>();

    private int CountDamagingEnemies()
    {
        var count = 0;
        foreach (var enemy in _enemiesInside)
        {
            if (enemy == null)
            {
                continue;
            }

            var freezable = enemy.GetComponent<Freezable>();
            if (freezable != null && freezable.IsFreezed)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private void TriggerDamageFlash()
    {
        EnsureDamageFlashTargets();
        ApplyDamageFlash(0f);

        if (_damageFlashRoutine != null)
        {
            StopCoroutine(_damageFlashRoutine);
        }

        _damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
    }

    private IEnumerator DamageFlashRoutine()
    {
        var duration = Mathf.Max(0.01f, damageFlashDuration);
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            ApplyDamageFlash(t);
            yield return null;
        }

        ApplyDamageFlash(1f);
        _damageFlashRoutine = null;
    }

    private void ApplyDamageFlash(float restoreT)
    {
        if (damageFlashRenderers == null || damageFlashRenderers.Length == 0)
        {
            return;
        }

        for (var i = 0; i < damageFlashRenderers.Length; i++)
        {
            var renderer = damageFlashRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            var block = _damageFlashBlocks[i];
            renderer.GetPropertyBlock(block);

            var color = Color.Lerp(damageFlashColor, _damageFlashBaseColors[i], restoreT);
            var propertyId = _damageFlashUsesBaseColor[i] ? BaseColorPropertyId : ColorPropertyId;
            block.SetColor(propertyId, color);
            renderer.SetPropertyBlock(block);
        }
    }

    private void EnsureDamageFlashTargets()
    {
        if (damageFlashRenderers == null || damageFlashRenderers.Length == 0)
        {
            var targets = new List<Renderer>();
            var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            for (var i = 0; i < meshRenderers.Length; i++)
            {
                targets.Add(meshRenderers[i]);
            }

            var skinnedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (var i = 0; i < skinnedRenderers.Length; i++)
            {
                targets.Add(skinnedRenderers[i]);
            }

            damageFlashRenderers = targets.ToArray();
        }

        if (damageFlashRenderers == null)
        {
            return;
        }

        if (_damageFlashBlocks != null && _damageFlashBlocks.Length == damageFlashRenderers.Length)
        {
            return;
        }

        _damageFlashBlocks = new MaterialPropertyBlock[damageFlashRenderers.Length];
        _damageFlashBaseColors = new Color[damageFlashRenderers.Length];
        _damageFlashUsesBaseColor = new bool[damageFlashRenderers.Length];

        for (var i = 0; i < damageFlashRenderers.Length; i++)
        {
            _damageFlashBlocks[i] = new MaterialPropertyBlock();

            var renderer = damageFlashRenderers[i];
            if (renderer == null || renderer.sharedMaterial == null)
            {
                _damageFlashBaseColors[i] = Color.white;
                continue;
            }

            var sharedMaterial = renderer.sharedMaterial;
            if (sharedMaterial.HasProperty(BaseColorPropertyId))
            {
                _damageFlashUsesBaseColor[i] = true;
                _damageFlashBaseColors[i] = sharedMaterial.GetColor(BaseColorPropertyId);
            }
            else if (sharedMaterial.HasProperty(ColorPropertyId))
            {
                _damageFlashUsesBaseColor[i] = false;
                _damageFlashBaseColors[i] = sharedMaterial.GetColor(ColorPropertyId);
            }
            else
            {
                _damageFlashUsesBaseColor[i] = true;
                _damageFlashBaseColors[i] = Color.white;
            }
        }
    }

    private void OnDisable()
    {
        _enemiesInside.Clear();
        ApplyDamageFlash(1f);

        if (_damageFlashRoutine != null)
        {
            StopCoroutine(_damageFlashRoutine);
            _damageFlashRoutine = null;
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(0f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        damagePerEnemyPerTick = Mathf.Max(0f, damagePerEnemyPerTick);
        damageInterval = Mathf.Max(0.01f, damageInterval);
        hitShakeIntensity = Mathf.Max(0f, hitShakeIntensity);
        damageFlashDuration = Mathf.Max(0.01f, damageFlashDuration);
    }
}
