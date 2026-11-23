using Framework;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class AudioManager : Singleton<AudioManager>
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";

    [Header("Levels (dB)")]
    [SerializeField] private float gameplayVolumeDb = 0f;
    [SerializeField] private float mutedVolumeDb = -80f;

    [Header("Fades")]
    [SerializeField] private float levelStartFadeInDuration = 1f;
    [SerializeField] private float restartFadeOutDuration = 0.3f;
    [SerializeField] private bool startMuted = true;
    [SerializeField] private bool useUnscaledTime = true;

    private Coroutine _fadeRoutine;

    protected override void OnAwake()
    {
        if (startMuted)
        {
            SetVolumeDirect(mutedVolumeDb);
        }
        else
        {
            SetVolumeDirect(gameplayVolumeDb);
        }
    }

    private void Start()
    {
        if (startMuted)
        {
            FadeToGameplay(levelStartFadeInDuration);
        }
    }

    public void FadeToGameplay(float duration)
    {
        FadeToDb(gameplayVolumeDb, duration);
    }

    public void FadeToMuted(float duration)
    {
        FadeToDb(mutedVolumeDb, duration);
    }

    public void FadeOutForRestart()
    {
        FadeToMuted(restartFadeOutDuration);
    }

    private void FadeToDb(float targetDb, float duration)
    {
        if (mixer == null || string.IsNullOrWhiteSpace(masterVolumeParameter))
        {
            return;
        }

        if (_fadeRoutine != null)
        {
            StopCoroutine(_fadeRoutine);
        }

        _fadeRoutine = StartCoroutine(FadeRoutine(targetDb, Mathf.Max(0.01f, duration)));
    }

    private System.Collections.IEnumerator FadeRoutine(float targetDb, float duration)
    {
        if (!mixer.GetFloat(masterVolumeParameter, out var startDb))
        {
            startDb = targetDb;
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var volumeDb = Mathf.Lerp(startDb, targetDb, t);
            mixer.SetFloat(masterVolumeParameter, volumeDb);
            yield return null;
        }

        mixer.SetFloat(masterVolumeParameter, targetDb);
        _fadeRoutine = null;
    }

    private void SetVolumeDirect(float valueDb)
    {
        if (mixer == null || string.IsNullOrWhiteSpace(masterVolumeParameter))
        {
            return;
        }

        mixer.SetFloat(masterVolumeParameter, valueDb);
    }

    private void OnValidate()
    {
        levelStartFadeInDuration = Mathf.Max(0.01f, levelStartFadeInDuration);
        restartFadeOutDuration = Mathf.Max(0.01f, restartFadeOutDuration);
    }
}
