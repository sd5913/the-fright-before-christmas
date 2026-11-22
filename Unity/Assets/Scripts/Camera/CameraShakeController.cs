using System.Collections.Generic;
using Framework;
using UnityEngine;

[DisallowMultipleComponent]
public class CameraShakeController : Singleton<CameraShakeController>
{
    private sealed class ActiveShake
    {
        public float Duration;
        public float Elapsed;
        public float PositionAmplitude;
        public Vector3 RotationAmplitude;
        public float Frequency;
        public Vector3 Seed;
    }

    [Header("Default Shake")]
    [SerializeField] private float defaultDuration = 0.16f;
    [SerializeField] private float defaultPositionAmplitude = 0.045f;
    [SerializeField] private Vector3 defaultRotationAmplitude = new Vector3(1.8f, 1.8f, 0.7f);
    [SerializeField] private float defaultFrequency = 28f;
    [SerializeField] private bool useUnscaledTime;

    private readonly List<ActiveShake> _activeShakes = new List<ActiveShake>();
    private Vector3 _lastPositionOffset;
    private Vector3 _lastRotationOffset;

    public void PlayDefaultShake(float intensityScale = 1f)
    {
        if (intensityScale <= 0f)
        {
            return;
        }

        Shake(
            defaultDuration,
            defaultPositionAmplitude * intensityScale,
            defaultRotationAmplitude * intensityScale,
            defaultFrequency);
    }

    public void Shake(float duration, float positionAmplitude, Vector3 rotationAmplitude, float frequency)
    {
        if (duration <= 0f || frequency <= 0f)
        {
            return;
        }

        _activeShakes.Add(new ActiveShake
        {
            Duration = duration,
            Elapsed = 0f,
            PositionAmplitude = Mathf.Max(0f, positionAmplitude),
            RotationAmplitude = rotationAmplitude,
            Frequency = frequency,
            Seed = new Vector3(
                Random.Range(0f, 1000f),
                Random.Range(0f, 1000f),
                Random.Range(0f, 1000f))
        });
    }

    private void LateUpdate()
    {
        RestorePreviousOffset();
        if (_activeShakes.Count == 0)
        {
            return;
        }

        var timeNow = useUnscaledTime ? Time.unscaledTime : Time.time;
        var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        var positionOffset = Vector3.zero;
        var rotationOffset = Vector3.zero;

        for (var i = _activeShakes.Count - 1; i >= 0; i--)
        {
            var shake = _activeShakes[i];
            shake.Elapsed += deltaTime;
            if (shake.Elapsed >= shake.Duration)
            {
                _activeShakes.RemoveAt(i);
                continue;
            }

            var progress = shake.Elapsed / shake.Duration;
            var fade = 1f - progress;
            fade *= fade;

            var phase = timeNow * shake.Frequency;
            var noise = new Vector3(
                NoiseSigned(phase + shake.Seed.x),
                NoiseSigned(phase + shake.Seed.y),
                NoiseSigned(phase + shake.Seed.z));

            positionOffset += noise * (shake.PositionAmplitude * fade);
            rotationOffset += Vector3.Scale(noise, shake.RotationAmplitude * fade);
        }

        transform.localPosition += positionOffset;
        transform.localRotation *= Quaternion.Euler(rotationOffset);

        _lastPositionOffset = positionOffset;
        _lastRotationOffset = rotationOffset;
    }

    private void RestorePreviousOffset()
    {
        if (_lastPositionOffset.sqrMagnitude <= Mathf.Epsilon && _lastRotationOffset.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        transform.localPosition -= _lastPositionOffset;
        transform.localRotation *= Quaternion.Inverse(Quaternion.Euler(_lastRotationOffset));

        _lastPositionOffset = Vector3.zero;
        _lastRotationOffset = Vector3.zero;
    }

    private static float NoiseSigned(float value)
    {
        return Mathf.PerlinNoise(value, value * 0.37f) * 2f - 1f;
    }

    private void OnValidate()
    {
        defaultDuration = Mathf.Max(0.01f, defaultDuration);
        defaultPositionAmplitude = Mathf.Max(0f, defaultPositionAmplitude);
        defaultFrequency = Mathf.Max(0.01f, defaultFrequency);
    }
}
