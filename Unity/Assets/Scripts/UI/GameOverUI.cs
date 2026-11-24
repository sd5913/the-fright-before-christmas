using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_cg;
    [SerializeField] private Image m_fadeImage;
    [SerializeField] private Transform m_punchTarget;
    [SerializeField] private Transform m_restartButtonPunchTarget;
    [SerializeField] private float m_punchAmount = 0.18f;
    [SerializeField] private float m_punchDuration = 0.2f;
    [SerializeField] private int m_punchVibrato = 2;
    [SerializeField] private float m_restartFadeDuration = 0.3f;
    [SerializeField] private float m_imageFadeDelay = 0f;
    [SerializeField] private float m_imageFadeDuration = 0.35f;
    [SerializeField, Range(0f, 1f)] private float m_imageTargetAlpha = 0.6f;

    [Space] [SerializeField] private TMP_InputField nameInput;

    private Coroutine _punchRoutine;
    private Coroutine _restartButtonPunchRoutine;
    private Coroutine _restartRoutine;
    private Coroutine _imageFadeRoutine;
    private Vector3 _baseScale = Vector3.one;
    private Vector3 _restartButtonBaseScale = Vector3.one;
    private bool _isRestarting;

    private void Awake()
    {
        var target = m_punchTarget != null ? m_punchTarget : transform;
        _baseScale = target.localScale;

        var restartTarget = GetRestartButtonTarget();
        if (restartTarget != null)
        {
            _restartButtonBaseScale = restartTarget.localScale;
        }
    }

    private void OnEnable()
    {
        _isRestarting = false;

        if (m_cg != null)
        {
            m_cg.alpha = 1f;
            m_cg.interactable = true;
            m_cg.blocksRaycasts = true;
        }

        if (m_fadeImage != null)
        {
            if (_imageFadeRoutine != null)
            {
                StopCoroutine(_imageFadeRoutine);
            }

            var color = m_fadeImage.color;
            color.a = 0f;
            m_fadeImage.color = color;
            _imageFadeRoutine = StartCoroutine(FadeImageToTargetAlpha());
        }

        if (_punchRoutine != null)
        {
            StopCoroutine(_punchRoutine);
        }

        _punchRoutine = StartCoroutine(PunchScaleRoutine(m_punchTarget != null ? m_punchTarget : transform, _baseScale, () => _punchRoutine = null));
    }

    private void OnDisable()
    {
        if (_punchRoutine != null)
        {
            StopCoroutine(_punchRoutine);
            _punchRoutine = null;
        }

        if (_restartButtonPunchRoutine != null)
        {
            StopCoroutine(_restartButtonPunchRoutine);
            _restartButtonPunchRoutine = null;
        }

        if (_restartRoutine != null)
        {
            StopCoroutine(_restartRoutine);
            _restartRoutine = null;
        }

        if (_imageFadeRoutine != null)
        {
            StopCoroutine(_imageFadeRoutine);
            _imageFadeRoutine = null;
        }

        var target = m_punchTarget != null ? m_punchTarget : transform;
        target.localScale = _baseScale;

        var restartTarget = GetRestartButtonTarget();
        if (restartTarget != null)
        {
            restartTarget.localScale = _restartButtonBaseScale;
        }
    }

    public void Restart()
    {
        if (_isRestarting)
        {
            return;
        }

        _isRestarting = true;

        var restartTarget = GetRestartButtonTarget();
        if (restartTarget != null)
        {
            if (_restartButtonPunchRoutine != null)
            {
                StopCoroutine(_restartButtonPunchRoutine);
            }

            _restartButtonPunchRoutine = StartCoroutine(PunchScaleRoutine(restartTarget, _restartButtonBaseScale, () => _restartButtonPunchRoutine = null));
        }

        if (_restartRoutine != null)
        {
            StopCoroutine(_restartRoutine);
        }

        _restartRoutine = StartCoroutine(RestartRoutine());
    }

    private IEnumerator FadeImageToTargetAlpha()
    {
        if (m_fadeImage == null)
        {
            _imageFadeRoutine = null;
            yield break;
        }

        var delay = Mathf.Max(0f, m_imageFadeDelay);
        if (delay > 0f)
        {
            var delayElapsed = 0f;
            while (delayElapsed < delay)
            {
                delayElapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, m_imageFadeDuration);
        var startColor = m_fadeImage.color;
        startColor.a = 0f;
        var targetAlpha = Mathf.Clamp01(m_imageTargetAlpha);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var color = startColor;
            color.a = Mathf.Lerp(0f, targetAlpha, t);
            m_fadeImage.color = color;
            yield return null;
        }

        var finalColor = m_fadeImage.color;
        finalColor.a = targetAlpha;
        m_fadeImage.color = finalColor;
        _imageFadeRoutine = null;
    }

    private IEnumerator RestartRoutine()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.FadeToMuted(m_restartFadeDuration);
        }

        if (m_cg != null)
        {
            m_cg.interactable = false;
            m_cg.blocksRaycasts = false;

            var startAlpha = m_cg.alpha;
            var elapsed = 0f;
            var duration = Mathf.Max(0.01f, m_restartFadeDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                m_cg.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            m_cg.alpha = 0f;
        }

        var activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private IEnumerator PunchScaleRoutine(Transform target, Vector3 baseScale, System.Action onComplete)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, m_punchDuration);
        var amount = Mathf.Max(0f, m_punchAmount);
        var vibrato = Mathf.Max(1, m_punchVibrato);

        target.localScale = baseScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var wave = Mathf.Sin(t * Mathf.PI * vibrato);
            var decay = 1f - t;
            var multiplier = 1f + wave * decay * amount;
            target.localScale = baseScale * multiplier;
            yield return null;
        }

        target.localScale = baseScale;
        onComplete?.Invoke();
    }

    private Transform GetRestartButtonTarget()
    {
        if (m_restartButtonPunchTarget != null)
        {
            return m_restartButtonPunchTarget;
        }

        return m_punchTarget != null ? m_punchTarget : transform;
    }

    private void OnValidate()
    {
        m_punchAmount = Mathf.Max(0f, m_punchAmount);
        m_punchDuration = Mathf.Max(0.01f, m_punchDuration);
        m_punchVibrato = Mathf.Max(1, m_punchVibrato);
        m_restartFadeDuration = Mathf.Max(0.01f, m_restartFadeDuration);
        m_imageFadeDelay = Mathf.Max(0f, m_imageFadeDelay);
        m_imageFadeDuration = Mathf.Max(0.01f, m_imageFadeDuration);
        m_imageTargetAlpha = Mathf.Clamp01(m_imageTargetAlpha);
    }

    public void SendScore()
    {
        //3
            
        // To do
        // Punch send score button
        // Add a text below the button displaying "sending..." or "failed/success!"
        // Make send score button interactable if the score has already been sent successfully
        // Or if the input field text does not contain anything
        
        // Add vacuum sfx
        // Add loot pickup sfx
        // Add game over stinger
        // Add button click sfx
        
        var score = LevelManager.Instance.Score;
        var playerName = nameInput.text;
        APIManager.SendScore(score.Value, playerName);
    }
}
