using System;
using System.Collections;
using System.Text;
using Framework;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : Singleton<APIManager>
{
    [SerializeField] private string scoresEndpoint = "https://metapool.cloud/tfbc-api/v1/scores";
    [SerializeField] private string runId;

    private string _sessionRunId;

    [Serializable]
    private sealed class ScorePayload
    {
        public string runId;
        public string playerName;
        public int score;
    }

    protected override void OnAwake()
    {
        _sessionRunId = string.IsNullOrWhiteSpace(runId) ? Guid.NewGuid().ToString("N") : runId.Trim();
    }

    [DarkButton]
    public void SendTestScore()
    {
        SendScore(10, "darketomaly");
    }
    
    public static void SendScore(int score, string playerName)
    {
        if (Instance == null)
        {
            Debug.LogError("APIManager.SendScore called before APIManager instance exists.");
            return;
        }

        Instance.StartCoroutine(Instance.SendScoreRoutine(score, playerName));
    }

    private IEnumerator SendScoreRoutine(int score, string playerName)
    {
        if (string.IsNullOrWhiteSpace(scoresEndpoint))
        {
            this.LogError("Score endpoint is empty.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(playerName))
        {
            this.LogError("Player name is empty; score not sent.");
            yield break;
        }

        var payload = new ScorePayload
        {
            runId = _sessionRunId,
            playerName = playerName.Trim(),
            score = Mathf.Max(0, score)
        };

        var json = JsonUtility.ToJson(payload);
        var body = Encoding.UTF8.GetBytes(json);

        using (var request = new UnityWebRequest(scoresEndpoint, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                this.LogError($"Failed sending score. HTTP {(long)request.responseCode}: {request.error}");
                yield break;
            }

            this.Log($"Score sent. HTTP {(long)request.responseCode}");
        }
    }
}