using UnityEngine;

[DisallowMultipleComponent]
public class CrossfadeLoopAudio : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private bool loopPlaylist = true;
    [SerializeField] private bool playOnEnable = true;

    [Header("Crossfade")]
    [SerializeField] private float crossfadeDuration = 1f;
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool useUnscaledTime;

    [Header("Source Settings")]
    [SerializeField] private bool spatial = false;
    [SerializeField] private float spatialBlend = 0f;

    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private Coroutine _playRoutine;
    private bool _isPlaying;

    private void Awake()
    {
        EnsureSources();
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        Stop();
    }

    public void Play()
    {
        EnsureSources();

        Stop();
        if (clips == null || clips.Length == 0)
        {
            return;
        }

        _isPlaying = true;
        _playRoutine = StartCoroutine(PlayRoutine());
    }

    public void Stop()
    {
        _isPlaying = false;

        if (_playRoutine != null)
        {
            StopCoroutine(_playRoutine);
            _playRoutine = null;
        }

        if (_sourceA != null)
        {
            _sourceA.Stop();
            _sourceA.volume = 0f;
        }

        if (_sourceB != null)
        {
            _sourceB.Stop();
            _sourceB.volume = 0f;
        }
    }

    private System.Collections.IEnumerator PlayRoutine()
    {
        var activeSource = _sourceA;
        var queuedSource = _sourceB;
        var currentIndex = 0;

        PlayClip(activeSource, clips[currentIndex], volume);

        while (_isPlaying)
        {
            var currentClip = clips[currentIndex];
            if (currentClip == null)
            {
                yield break;
            }

            var nextIndex = GetNextClipIndex(currentIndex);
            if (nextIndex < 0)
            {
                yield return WaitSeconds(currentClip.length);
                break;
            }

            var fadeDuration = Mathf.Clamp(crossfadeDuration, 0.01f, currentClip.length * 0.5f);
            var preFadeTime = Mathf.Max(0f, currentClip.length - fadeDuration);
            yield return WaitSeconds(preFadeTime);
            if (!_isPlaying)
            {
                yield break;
            }

            PlayClip(queuedSource, clips[nextIndex], 0f);

            var elapsed = 0f;
            while (elapsed < fadeDuration && _isPlaying)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / fadeDuration);
                activeSource.volume = Mathf.Lerp(volume, 0f, t);
                queuedSource.volume = Mathf.Lerp(0f, volume, t);
                yield return null;
            }

            activeSource.Stop();
            activeSource.volume = 0f;
            queuedSource.volume = volume;

            var tmp = activeSource;
            activeSource = queuedSource;
            queuedSource = tmp;
            currentIndex = nextIndex;
        }

        _playRoutine = null;
        _isPlaying = false;
    }

    private int GetNextClipIndex(int currentIndex)
    {
        if (clips == null || clips.Length == 0)
        {
            return -1;
        }

        if (clips.Length == 1)
        {
            return loopPlaylist ? 0 : -1;
        }

        var nextIndex = currentIndex + 1;
        if (nextIndex >= clips.Length)
        {
            return loopPlaylist ? 0 : -1;
        }

        return nextIndex;
    }

    private void PlayClip(AudioSource source, AudioClip clip, float clipVolume)
    {
        source.clip = clip;
        source.loop = false;
        source.volume = Mathf.Clamp01(clipVolume);
        source.time = 0f;
        source.Play();
    }

    private System.Collections.IEnumerator WaitSeconds(float duration)
    {
        var remaining = Mathf.Max(0f, duration);
        while (remaining > 0f && _isPlaying)
        {
            remaining -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }

    private void EnsureSources()
    {
        if (_sourceA == null)
        {
            _sourceA = GetComponent<AudioSource>();
            if (_sourceA == null)
            {
                _sourceA = gameObject.AddComponent<AudioSource>();
            }
        }

        if (_sourceB == null)
        {
            _sourceB = gameObject.AddComponent<AudioSource>();
        }

        ConfigureSource(_sourceA);
        ConfigureSource(_sourceB);
    }

    private void ConfigureSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = spatial ? Mathf.Clamp01(spatialBlend) : 0f;
    }

    private void OnValidate()
    {
        crossfadeDuration = Mathf.Max(0.01f, crossfadeDuration);
        volume = Mathf.Clamp01(volume);
        spatialBlend = Mathf.Clamp01(spatialBlend);
    }
}
