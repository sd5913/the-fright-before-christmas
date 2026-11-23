using System;
using Framework.Events;
using TMPro;
using UnityEngine;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_scoreText;

    private void Awake()
    {
        m_scoreText.text = "Score: 0";
    }

    private void OnEnable()
    {
        FrameworkEventsFactory.Subscribe(GameEvent.GameReady, OnGameReady);
    }

    private void OnDisable()
    {
        FrameworkEventsFactory.Unsubscribe(GameEvent.GameReady, OnGameReady);

        if (LevelManager.Instance)
        {
            LevelManager.Instance.Score.ValueChanged += OnScoreValueChanged;
        }
    }
    
    private void OnGameReady()
    {
        LevelManager.Instance.Score.ValueChanged += OnScoreValueChanged;
    }

    private void OnScoreValueChanged(int score)
    {
        // To do
        // Punch
        
        m_scoreText.text = $"Score: {score}";
    }
}
