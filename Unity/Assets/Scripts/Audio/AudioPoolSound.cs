using UnityEngine;

[CreateAssetMenu(fileName = "AudioPoolSound", menuName = "Audio/Pool Sound")]
public class AudioPoolSound : ScriptableObject
{
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private Vector2 pitchRange = new Vector2(1f, 1f);
    [SerializeField] private float volume = 1f;

    public AudioClip[] Clips => clips;
    public Vector2 PitchRange => pitchRange;
    public float Volume => volume;

    private void OnValidate()
    {
        if (pitchRange.x > pitchRange.y)
        {
            (pitchRange.x, pitchRange.y) = (pitchRange.y, pitchRange.x);
        }

        volume = Mathf.Clamp01(volume);
    }
}
