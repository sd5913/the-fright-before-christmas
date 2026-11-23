using System;
using System.Collections;
using Framework.Events;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_scoreText;
    [SerializeField] private float m_punchAmount = 0.18f;
    [SerializeField] private float m_punchDuration = 0.16f;
    [SerializeField] private int m_punchVibrato = 2;

    private Coroutine _punchRoutine;
    private Vector3 _baseScale = Vector3.one;

    private void Awake()
    {
        if (m_scoreText == null)
        {
            return;
        }

        _baseScale = m_scoreText.transform.localScale;
        m_scoreText.text = "Score: 0";
    }

    private void OnEnable()
    {
        FrameworkEventsFactory.Subscribe(GameEvent.GameReady, OnGameReady);

        if (LevelManager.Instance)
        {
            OnScoreValueChanged(LevelManager.Instance.Score.Value);
        }
    }

    private void OnDisable()
    {
        FrameworkEventsFactory.Unsubscribe(GameEvent.GameReady, OnGameReady);

        if (LevelManager.Instance)
        {
            LevelManager.Instance.Score.ValueChanged -= OnScoreValueChanged;
        }

        if (_punchRoutine != null)
        {
            StopCoroutine(_punchRoutine);
            _punchRoutine = null;
        }

        if (m_scoreText != null)
        {
            m_scoreText.transform.localScale = _baseScale;
        }
    }
    
    private void OnGameReady()
    {
        LevelManager.Instance.Score.ValueChanged -= OnScoreValueChanged;
        LevelManager.Instance.Score.ValueChanged += OnScoreValueChanged;
    }

    private void OnScoreValueChanged(int score)
    {
        if (m_scoreText == null)
        {
            return;
        }

        if (_punchRoutine != null)
        {
            StopCoroutine(_punchRoutine);
        }

        _punchRoutine = StartCoroutine(PunchScoreRoutine());
        m_scoreText.text = $"Score: {score}";
    }

    private IEnumerator PunchScoreRoutine()
    {
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, m_punchDuration);
        var amount = Mathf.Max(0f, m_punchAmount);
        var vibrato = Mathf.Max(1, m_punchVibrato);

        while (elapsed < duration && m_scoreText != null)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var wave = Mathf.Sin(t * Mathf.PI * vibrato);
            var decay = 1f - t;
            var multiplier = 1f + wave * decay * amount;
            m_scoreText.transform.localScale = _baseScale * multiplier;
            yield return null;
        }

        if (m_scoreText != null)
        {
            m_scoreText.transform.localScale = _baseScale;
        }

        _punchRoutine = null;
    }

    private void OnValidate()
    {
        m_punchAmount = Mathf.Max(0f, m_punchAmount);
        m_punchDuration = Mathf.Max(0.01f, m_punchDuration);
        m_punchVibrato = Mathf.Max(1, m_punchVibrato);
    }
}
