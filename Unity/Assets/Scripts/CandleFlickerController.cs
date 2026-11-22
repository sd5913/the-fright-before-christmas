using UnityEngine;

[DisallowMultipleComponent]
public class CandleFlickerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Light targetLight;
    [SerializeField] private Renderer[] emissionRenderers;
    [SerializeField] private int materialIndex = -1;

    [Header("Emission")]
    [SerializeField] private string emissionColorProperty = "_EmissionColor";
    [SerializeField] private Color emissionColor = new Color(1f, 0.65f, 0.3f);
    [SerializeField] private Vector2 emissionIntensityRange = new Vector2(1.8f, 3.6f);

    [Header("Light")]
    [SerializeField] private Vector2 lightIntensityRange = new Vector2(1.1f, 1.8f);
    [SerializeField] private Vector2 lightRangeRange = new Vector2(2.5f, 3.2f);

    [Header("Flicker Motion")]
    [SerializeField] private float primarySpeed = 2.2f;
    [SerializeField] private float secondarySpeed = 7.5f;
    [SerializeField, Range(0f, 1f)] private float secondaryBlend = 0.35f;
    [SerializeField] private float smoothing = 18f;

    private int _emissionColorPropertyId;
    private float _seed;
    private float _normalizedFlicker;
    private MaterialPropertyBlock[] _propertyBlocks;

    private void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponentInChildren<Light>();
        }

        if (emissionRenderers == null || emissionRenderers.Length == 0)
        {
            emissionRenderers = GetComponentsInChildren<Renderer>(true);
        }

        _seed = Random.value * 1000f;
        _normalizedFlicker = 1f;
        RebuildPropertyBlocks();
        CachePropertyId();
        ApplyFlicker(1f);
    }

    private void Update()
    {
        var time = Time.time;
        var primary = Mathf.PerlinNoise(_seed, time * primarySpeed);
        var secondary = Mathf.PerlinNoise(_seed * 1.73f, time * secondarySpeed);
        var target = Mathf.Lerp(primary, secondary, secondaryBlend);

        _normalizedFlicker = Mathf.Lerp(_normalizedFlicker, target, Time.deltaTime * smoothing);
        ApplyFlicker(_normalizedFlicker);
    }

    private void ApplyFlicker(float normalized)
    {
        var clamped = Mathf.Clamp01(normalized);

        if (targetLight != null)
        {
            targetLight.intensity = Mathf.Lerp(lightIntensityRange.x, lightIntensityRange.y, clamped);
            targetLight.range = Mathf.Lerp(lightRangeRange.x, lightRangeRange.y, clamped);
        }

        if (emissionRenderers == null)
        {
            return;
        }

        var color = emissionColor * Mathf.Lerp(emissionIntensityRange.x, emissionIntensityRange.y, clamped);
        for (var i = 0; i < emissionRenderers.Length; i++)
        {
            var renderer = emissionRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            var block = _propertyBlocks != null && i < _propertyBlocks.Length ? _propertyBlocks[i] : null;
            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            if (materialIndex >= 0)
            {
                if (materialIndex >= renderer.sharedMaterials.Length)
                {
                    continue;
                }

                renderer.GetPropertyBlock(block, materialIndex);
                block.SetColor(_emissionColorPropertyId, color);
                renderer.SetPropertyBlock(block, materialIndex);
            }
            else
            {
                renderer.GetPropertyBlock(block);
                block.SetColor(_emissionColorPropertyId, color);
                renderer.SetPropertyBlock(block);
            }

            if (_propertyBlocks != null && i < _propertyBlocks.Length)
            {
                _propertyBlocks[i] = block;
            }
        }
    }

    private void CachePropertyId()
    {
        _emissionColorPropertyId = string.IsNullOrWhiteSpace(emissionColorProperty)
            ? Shader.PropertyToID("_EmissionColor")
            : Shader.PropertyToID(emissionColorProperty);
    }

    private void RebuildPropertyBlocks()
    {
        if (emissionRenderers == null)
        {
            _propertyBlocks = null;
            return;
        }

        _propertyBlocks = new MaterialPropertyBlock[emissionRenderers.Length];
        for (var i = 0; i < _propertyBlocks.Length; i++)
        {
            _propertyBlocks[i] = new MaterialPropertyBlock();
        }
    }

    private void OnValidate()
    {
        CachePropertyId();
        SortRange(ref emissionIntensityRange);
        SortRange(ref lightIntensityRange);
        SortRange(ref lightRangeRange);

        primarySpeed = Mathf.Max(0f, primarySpeed);
        secondarySpeed = Mathf.Max(0f, secondarySpeed);
        smoothing = Mathf.Max(0f, smoothing);

        if (emissionRenderers != null && (_propertyBlocks == null || _propertyBlocks.Length != emissionRenderers.Length))
        {
            RebuildPropertyBlocks();
        }
    }

    private static void SortRange(ref Vector2 range)
    {
        if (range.x > range.y)
        {
            (range.x, range.y) = (range.y, range.x);
        }
    }
}
