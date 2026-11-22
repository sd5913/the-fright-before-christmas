using System.Collections.Generic;
using Framework;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CameraFeedback : Singleton<CameraFeedback>
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
    
    [Header("Focused Character Follow")]
    [SerializeField] private bool followFocusedCharacterX = true;
    [SerializeField] private CharacterSwapper characterSwapper;
    [SerializeField] private float followLerpSpeed = 8f;
    [SerializeField] private float followXOffset;

    [Header("Intro Movement")]
    [SerializeField] private bool playIntroMovement = true;
    [SerializeField] private float introStartDelay = 1f;
    [SerializeField] private float introLength = 1.25f;
    [SerializeField] private float introDuration = 0.75f;
    [SerializeField] private AnimationCurve introEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private Image introFadeImage;
    [SerializeField] private SpriteRenderer introLogoSprite;
    [SerializeField] private float introLogoFadeDelay = 0f;
    [SerializeField] private float introLogoFadeDuration = 0.5f;

    private readonly List<ActiveShake> _activeShakes = new List<ActiveShake>();
    private Vector3 _lastPositionOffset;
    private Vector3 _lastRotationOffset;
    private Vector3 _introStartPosition;
    private Vector3 _introTargetPosition;
    private float _introElapsed;
    private float _introDelayRemaining;
    private bool _introActive;
    private bool _logoFadeActive;
    private float _logoFadeElapsed;
    private float _logoFadeStartAlpha;

    private void Start()
    {
        _introStartPosition = transform.position;
        _introTargetPosition = _introStartPosition - transform.forward * introLength;
        _introElapsed = 0f;
        _introDelayRemaining = introStartDelay;
        _introActive = playIntroMovement && introLength > 0f && introDuration > 0f;
        UpdateIntroFadeAlpha(_introActive ? 1f : 0f);
    }

    public void PlayDefaultShake(float intensityScale = 1f)
    {
        if (intensityScale <= 0f || _introActive)
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
        if (duration <= 0f || frequency <= 0f || _introActive)
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

        if (_introActive)
        {
            UpdateIntro();
            return;
        }

        UpdateLogoFadeOut();
        UpdateFollowX();

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

    private void UpdateIntro()
    {
        if (_introDelayRemaining > 0f)
        {
            transform.position = _introStartPosition;
            UpdateIntroFadeAlpha(1f);
            var delayDelta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _introDelayRemaining -= delayDelta;
            return;
        }

        var progress = Mathf.Clamp01(_introElapsed / introDuration);
        var eased = introEase != null ? introEase.Evaluate(progress) : progress;
        transform.position = Vector3.LerpUnclamped(_introStartPosition, _introTargetPosition, eased);
        UpdateIntroFadeAlpha(1f - progress);

        if (progress >= 1f)
        {
            UpdateIntroFadeAlpha(0f);
            _introActive = false;
            StartLogoFadeOut();
            return;
        }

        var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _introElapsed += deltaTime;
    }

    private void StartLogoFadeOut()
    {
        if (introLogoSprite == null || introLogoFadeDuration <= 0f)
        {
            return;
        }

        _logoFadeStartAlpha = introLogoSprite.color.a;
        _logoFadeElapsed = -introLogoFadeDelay;
        _logoFadeActive = true;
    }

    private void UpdateLogoFadeOut()
    {
        if (!_logoFadeActive || introLogoSprite == null)
        {
            return;
        }

        var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        _logoFadeElapsed += deltaTime;
        if (_logoFadeElapsed < 0f)
        {
            return;
        }

        var progress = Mathf.Clamp01(_logoFadeElapsed / introLogoFadeDuration);
        var color = introLogoSprite.color;
        color.a = Mathf.Lerp(_logoFadeStartAlpha, 0f, progress);
        introLogoSprite.color = color;

        if (progress >= 1f)
        {
            _logoFadeActive = false;
        }
    }

    private void UpdateIntroFadeAlpha(float alpha)
    {
        if (introFadeImage == null)
        {
            return;
        }

        var color = introFadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        introFadeImage.color = color;
    }

    private void UpdateFollowX()
    {
        if (!followFocusedCharacterX)
        {
            return;
        }

        var swapper = characterSwapper != null ? characterSwapper : FindAnyObjectByType<CharacterSwapper>();
        if (swapper == null || swapper.FocusedCharacter == null)
        {
            return;
        }

        characterSwapper = swapper;

        var targetX = swapper.FocusedCharacter.transform.position.x + followXOffset;
        var current = transform.position;
        var deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        var t = followLerpSpeed <= 0f ? 1f : 1f - Mathf.Exp(-followLerpSpeed * deltaTime);
        current.x = Mathf.Lerp(current.x, targetX, t);
        transform.position = current;
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
        followLerpSpeed = Mathf.Max(0f, followLerpSpeed);
        introStartDelay = Mathf.Max(0f, introStartDelay);
        introLength = Mathf.Max(0f, introLength);
        introDuration = Mathf.Max(0.01f, introDuration);
        introLogoFadeDelay = Mathf.Max(0f, introLogoFadeDelay);
        introLogoFadeDuration = Mathf.Max(0.01f, introLogoFadeDuration);
    }
}
