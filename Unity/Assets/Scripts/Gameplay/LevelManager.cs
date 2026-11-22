using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public bool GameReady { get; private set; }
    public bool GameOver { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        GameReady = false;
        GameOver = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetGameReady(bool value)
    {
        GameReady = value;
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
