using Framework.Events;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_HudCg;
    [SerializeField] private CanvasGroup m_GameOverCg;
    [SerializeField] private float m_hudFadeLerpSpeed = 10f;

    private float _targetHudAlpha;

    private void Awake()
    {
        if (m_HudCg == null)
        {
            return;
        }

        m_HudCg.alpha = 0f;
        m_GameOverCg.gameObject.SetActive(false);
        _targetHudAlpha = 0f;
    }

    private void OnEnable()
    {
        FrameworkEventsFactory.Subscribe(GameEvent.GameReady, OnGameReady);
        FrameworkEventsFactory.Subscribe(GameEvent.GameOver, OnGameOver);
    }


    private void OnDisable()
    {
        FrameworkEventsFactory.Unsubscribe(GameEvent.GameReady, OnGameReady);
        FrameworkEventsFactory.Unsubscribe(GameEvent.GameOver, OnGameOver);
    }

    private void Update()
    {
        if (m_HudCg == null)
        {
            return;
        }

        var t = 1f - Mathf.Exp(-Mathf.Max(0f, m_hudFadeLerpSpeed) * Time.deltaTime);
        m_HudCg.alpha = Mathf.Lerp(m_HudCg.alpha, _targetHudAlpha, t);
    }

    private void OnGameReady()
    {
        _targetHudAlpha = 1f;
    }
    
    private void OnGameOver()
    {
        _targetHudAlpha = 0f;
        m_GameOverCg.gameObject.SetActive(true);
    }

    private void OnValidate()
    {
        m_hudFadeLerpSpeed = Mathf.Max(0f, m_hudFadeLerpSpeed);
    }
}
