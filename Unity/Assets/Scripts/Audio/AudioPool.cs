using System.Collections.Generic;
using Framework;
using UnityEngine;
using UnityEngine.Audio;

[DisallowMultipleComponent]
public class AudioPool : Singleton<AudioPool>
{
    [SerializeField] private AudioMixerGroup outputMixerGroup;
    [SerializeField] private int initialPoolSize = 8;
    [SerializeField] private int maxPoolSize = 24;

    private readonly List<AudioSource> _sources = new List<AudioSource>();

    protected override void OnAwake()
    {
        EnsurePool(Mathf.Max(1, initialPoolSize));
    }

    public AudioSource Play(AudioPoolSound sound)
    {
        if (sound == null)
        {
            return null;
        }

        var clip = GetRandomClip(sound.Clips);
        if (clip == null)
        {
            return null;
        }

        var source = GetAvailableSource();
        if (source == null)
        {
            return null;
        }

        source.clip = clip;
        source.volume = Mathf.Clamp01(sound.Volume);
        source.pitch = Random.Range(sound.PitchRange.x, sound.PitchRange.y);
        source.time = 0f;
        source.Play();
        return source;
    }

    public AudioSource play(AudioPoolSound sound)
    {
        return Play(sound);
    }

    private static AudioClip GetRandomClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        var validCount = 0;
        for (var i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        var target = Random.Range(0, validCount);
        for (var i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null)
            {
                continue;
            }

            if (target == 0)
            {
                return clips[i];
            }

            target--;
        }

        return null;
    }

    private AudioSource GetAvailableSource()
    {
        for (var i = 0; i < _sources.Count; i++)
        {
            var source = _sources[i];
            if (source != null && !source.isPlaying)
            {
                return source;
            }
        }

        if (_sources.Count >= Mathf.Max(1, maxPoolSize))
        {
            return null;
        }

        return CreateSource();
    }

    private void EnsurePool(int targetCount)
    {
        while (_sources.Count < targetCount)
        {
            CreateSource();
        }
    }

    private AudioSource CreateSource()
    {
        var go = new GameObject($"AudioPoolSource_{_sources.Count}");
        go.transform.SetParent(transform, false);

        var source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = outputMixerGroup;
        _sources.Add(source);
        return source;
    }

    private void OnValidate()
    {
        initialPoolSize = Mathf.Max(1, initialPoolSize);
        maxPoolSize = Mathf.Max(initialPoolSize, maxPoolSize);
    }
}
