using System;
using System.Collections;
using System.Collections.Generic;
using Framework;
using UnityEngine;

public class Health : MonoBehaviour
{
    private static readonly int BaseColorPropertyId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");

    public float CurrentHealth => currentHealth;

    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float currentHealth = 3f;
    [SerializeField] private float hitShakeIntensity = 1f;
    [SerializeField] private Color damageFlashColor = Color.red;
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private Renderer[] damageFlashRenderers;

    private Coroutine _damageFlashRoutine;
    private MaterialPropertyBlock[] _damageFlashBlocks;
    private Color[] _damageFlashBaseColors;
    private bool[] _damageFlashUsesBaseColor;

    public Action onDeath;

    private void Awake()
    {
        maxHealth = Mathf.Max(0f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        EnsureDamageFlashTargets();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        var damager = other.GetComponent<Damager>() ?? other.GetComponentInParent<Damager>();
        if (damager == null || damager.gameObject == gameObject)
        {
            return;
        }

        Destroy(damager.gameObject);

        if (currentHealth <= 0f)
        {
            return;
        }

        ApplyDamage(damager.Damage);
    }

    public void ApplyDamage(float damage)
    {
        if (damage <= 0f || currentHealth <= 0f)
        {
            if (currentHealth <= 0)
            {
                this.LogError("Already dead");
            }
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);
        TriggerDamageFlash();

        if (CameraFeedback.Instance != null && hitShakeIntensity > 0f)
        {
            CameraFeedback.Instance.PlayDefaultShake(hitShakeIntensity);
        }
        
        if (currentHealth <= 0f)
        {
            onDeath?.Invoke();
            Destroy(gameObject);
        }
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
        hitShakeIntensity = Mathf.Max(0f, hitShakeIntensity);
        damageFlashDuration = Mathf.Max(0.01f, damageFlashDuration);
    }
}
