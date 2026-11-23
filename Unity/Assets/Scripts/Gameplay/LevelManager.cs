using System;
using Framework;
using Framework.Events;
using UnityEngine;

public enum GameEvent
{
    GameReady,
    GameOver,
}

public class LevelManager : Singleton<LevelManager>
{
    public bool GameReady { get; private set; }
    public bool GameOver { get; private set; }
    public PropertyValue<int> Score { get; private set; } = new();

    protected override void OnAwake()
    {
        GameReady = false;
        GameOver = false;
    }

    private void OnEnable()
    {
        LootItem.OnLootCollected += OnLootCollected;
    }

    private void OnDisable()
    {
        LootItem.OnLootCollected -= OnLootCollected;
        FrameworkEventsFactory.Unraise(GameEvent.GameReady);
        FrameworkEventsFactory.Unraise(GameEvent.GameOver);
    }
    
    private void OnLootCollected()
    {
        Score.Value++;
    }

    public void SetGameReady(bool value)
    {
        GameReady = value;

        if (value)
        {
            FrameworkEventsFactory.Raise(GameEvent.GameReady);
        }
    }

    public void SetGameOver(bool value)
    {
        GameOver = value;
        
        if (value)
        {
            GameReady = false;
            FrameworkEventsFactory.Raise(GameEvent.GameOver);
        }
    }
}