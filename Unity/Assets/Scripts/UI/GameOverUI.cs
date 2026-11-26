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
    [SerializeField] private AudioPoolSound m_buttonClickSound;

    [Space] [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private TMP_Text sendFeedbackText;
    [SerializeField] private float feedbackFadeDuration = 0.18f;
    [SerializeField] private float feedbackVisibleDuration = 1.1f;
    [SerializeField] private float feedbackPunchAmount = 0.12f;
    [SerializeField] private float feedbackPunchDuration = 0.16f;
    [SerializeField] private int feedbackPunchVibrato = 2;

    private Coroutine _punchRoutine;
    private Coroutine _restartButtonPunchRoutine;
    private Coroutine _restartRoutine;
    private Coroutine _imageFadeRoutine;
    private Coroutine _feedbackRoutine;
    private Coroutine _feedbackPunchRoutine;
    private Vector3 _baseScale = Vector3.one;
    private Vector3 _restartButtonBaseScale = Vector3.one;
    private Vector3 _feedbackBaseScale = Vector3.one;
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

        EnsureFeedbackText();
        if (sendFeedbackText != null)
        {
            _feedbackBaseScale = sendFeedbackText.transform.localScale;
            SetFeedbackVisual(string.Empty, Color.white, 0f);
        }
    }

    private void OnEnable()
    {
        _isRestarting = false;
        APIManager.OnSendScoreStarted += OnSendScoreStarted;
        APIManager.OnSendScoreCompleted += OnSendScoreCompleted;

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
        APIManager.OnSendScoreStarted -= OnSendScoreStarted;
        APIManager.OnSendScoreCompleted -= OnSendScoreCompleted;

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

        if (_feedbackRoutine != null)
        {
            StopCoroutine(_feedbackRoutine);
            _feedbackRoutine = null;
        }

        if (_feedbackPunchRoutine != null)
        {
            StopCoroutine(_feedbackPunchRoutine);
            _feedbackPunchRoutine = null;
        }

        var target = m_punchTarget != null ? m_punchTarget : transform;
        target.localScale = _baseScale;

        var restartTarget = GetRestartButtonTarget();
        if (restartTarget != null)
        {
            restartTarget.localScale = _restartButtonBaseScale;
        }

        if (sendFeedbackText != null)
        {
            sendFeedbackText.transform.localScale = _feedbackBaseScale;
            SetFeedbackVisual(string.Empty, Color.white, 0f);
        }
    }

    public void Restart()
    {
        PlayButtonClickSound();

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
        yield return PunchScaleRoutine(target, baseScale, onComplete, m_punchAmount, m_punchDuration, m_punchVibrato);
    }

    private IEnumerator PunchScaleRoutine(Transform target, Vector3 baseScale, System.Action onComplete, float amount, float duration, int vibrato)
    {
        if (target == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        var elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        amount = Mathf.Max(0f, amount);
        vibrato = Mathf.Max(1, vibrato);

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
        feedbackFadeDuration = Mathf.Max(0.01f, feedbackFadeDuration);
        feedbackVisibleDuration = Mathf.Max(0f, feedbackVisibleDuration);
        feedbackPunchAmount = Mathf.Max(0f, feedbackPunchAmount);
        feedbackPunchDuration = Mathf.Max(0.01f, feedbackPunchDuration);
        feedbackPunchVibrato = Mathf.Max(1, feedbackPunchVibrato);
    }

    public void SendScore()
    {
        PlayButtonClickSound();

        var score = LevelManager.Instance.Score;
        var playerName = nameInput.text;
        APIManager.SendScore(score.Value, playerName);
    }

    private void OnSendScoreStarted()
    {
        ShowFeedback("Sending...", new Color(1f, 0.85f, 0.35f, 1f), true);
    }

    private void OnSendScoreCompleted(bool success, string message)
    {
        var color = success ? new Color(0.45f, 1f, 0.45f, 1f) : new Color(1f, 0.45f, 0.45f, 1f);
        ShowFeedback(string.IsNullOrWhiteSpace(message) ? (success ? "Score submitted!" : "Failed to submit score.") : message, color, false);
    }

    private void ShowFeedback(string message, Color color, bool keepVisible)
    {
        EnsureFeedbackText();
        if (sendFeedbackText == null)
        {
            return;
        }

        if (_feedbackRoutine != null)
        {
            StopCoroutine(_feedbackRoutine);
        }

        if (_feedbackPunchRoutine != null)
        {
            StopCoroutine(_feedbackPunchRoutine);
            _feedbackPunchRoutine = null;
        }

        _feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(message, color, keepVisible));
    }

    private IEnumerator ShowFeedbackRoutine(string message, Color color, bool keepVisible)
    {
        if (sendFeedbackText == null)
        {
            _feedbackRoutine = null;
            yield break;
        }

        sendFeedbackText.transform.localScale = _feedbackBaseScale;
        SetFeedbackVisual(message, color, 0f);

        _feedbackPunchRoutine = StartCoroutine(PunchScaleRoutine(sendFeedbackText.transform, _feedbackBaseScale, () => _feedbackPunchRoutine = null, feedbackPunchAmount, feedbackPunchDuration, feedbackPunchVibrato));

        var fadeDuration = Mathf.Max(0.01f, feedbackFadeDuration);
        var elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / fadeDuration);
            SetFeedbackVisual(message, color, t);
            yield return null;
        }

        SetFeedbackVisual(message, color, 1f);

        if (!keepVisible)
        {
            var hold = Mathf.Max(0f, feedbackVisibleDuration);
            var holdElapsed = 0f;
            while (holdElapsed < hold)
            {
                holdElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                SetFeedbackVisual(message, color, t);
                yield return null;
            }

            SetFeedbackVisual(string.Empty, color, 0f);
        }

        _feedbackRoutine = null;
    }

    private void PlayButtonClickSound()
    {
        if (m_buttonClickSound == null || AudioPool.Instance == null)
        {
            return;
        }

        AudioPool.Instance.play(m_buttonClickSound);
    }

    private void EnsureFeedbackText()
    {
        if (sendFeedbackText != null || nameInput == null)
        {
            return;
        }

        var existing = nameInput.transform.Find("Send Feedback Text");
        if (existing != null)
        {
            sendFeedbackText = existing.GetComponent<TMP_Text>();
            if (sendFeedbackText != null)
            {
                return;
            }
        }

        var go = new GameObject("Send Feedback Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(nameInput.transform, false);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -8f);
        rect.sizeDelta = new Vector2(0f, 28f);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 24f;
        text.color = Color.white;
        text.raycastTarget = false;
        sendFeedbackText = text;
    }

    private void SetFeedbackVisual(string message, Color color, float alpha)
    {
        if (sendFeedbackText == null)
        {
            return;
        }

        sendFeedbackText.text = message;
        var c = color;
        c.a = Mathf.Clamp01(alpha);
        sendFeedbackText.color = c;
    }
}
