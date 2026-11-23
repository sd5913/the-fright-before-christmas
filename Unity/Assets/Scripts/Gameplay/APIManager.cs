using Framework;
using UnityEngine;

public class APIManager : Singleton<APIManager>
{
    public static void SendScore(int score, string playerName)
    {
        Instance.Log($"Sending {score} for {playerName}");
    }
}