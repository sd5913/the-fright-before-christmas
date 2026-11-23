using System;
using Framework;
using Framework.Events;
using UnityEngine;

public enum GameEvent
{
    GameReady,
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
    }
    
    private void OnLootCollected()
    {
        Score.Value++;
        this.Log($"Loot collected, score: {Score}");
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
        }
    }
}