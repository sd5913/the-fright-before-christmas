using System;
using System.Collections;
using System.Text;
using Framework;
using UnityEngine;
using UnityEngine.Networking;

public class APIManager : Singleton<APIManager>
{
    public static event Action OnSendScoreStarted;
    public static event Action<bool, string> OnSendScoreCompleted;

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

    [Serializable]
    private sealed class ApiErrorEnvelope
    {
        public ApiError error;
    }

    [Serializable]
    private sealed class ApiError
    {
        public string code;
        public string message;
    }

    protected override void OnAwake()
    {
        _sessionRunId = string.IsNullOrWhiteSpace(runId) ? Guid.NewGuid().ToString("D") : runId.Trim();
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
            const string message = "API manager not available.";
            Debug.LogError($"APIManager.SendScore failed: {message}");
            OnSendScoreCompleted?.Invoke(false, message);
            return;
        }

        OnSendScoreStarted?.Invoke();
        Instance.StartCoroutine(Instance.SendScoreRoutine(score, playerName));
    }

    private IEnumerator SendScoreRoutine(int score, string playerName)
    {
        if (string.IsNullOrWhiteSpace(scoresEndpoint))
        {
            const string message = "Score endpoint is empty.";
            this.LogError(message);
            OnSendScoreCompleted?.Invoke(false, message);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(playerName))
        {
            const string message = "Player name is required.";
            this.LogError(message);
            OnSendScoreCompleted?.Invoke(false, message);
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
                var responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
                var backendMessage = ExtractBackendErrorMessage(responseText);
                var errorMessage = string.IsNullOrWhiteSpace(backendMessage)
                    ? $"Failed to send score ({(long)request.responseCode})."
                    : $"Failed to send score ({(long)request.responseCode}): {backendMessage}";

                this.LogError($"{errorMessage} UnityError={request.error} Response={responseText}");
                OnSendScoreCompleted?.Invoke(false, errorMessage);
                yield break;
            }

            this.Log($"Score sent. HTTP {(long)request.responseCode}");
            OnSendScoreCompleted?.Invoke(true, "Score submitted!");
        }
    }

    private static string ExtractBackendErrorMessage(string responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return string.Empty;
        }

        var envelope = JsonUtility.FromJson<ApiErrorEnvelope>(responseText);
        if (envelope != null && envelope.error != null && !string.IsNullOrWhiteSpace(envelope.error.message))
        {
            return envelope.error.message.Trim();
        }

        return string.Empty;
    }
}