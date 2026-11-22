using UnityEngine;

[DisallowMultipleComponent]
public class AutoDestroy : MonoBehaviour
{
    [SerializeField] private float lifetimeSeconds = 6f;
    [SerializeField] private bool useUnscaledTime;
    [SerializeField] private bool startPaused;

    private float _remainingLifetime;
    private bool _isPaused;

    private void Awake()
    {
        _remainingLifetime = Mathf.Max(0f, lifetimeSeconds);
        _isPaused = startPaused;
    }

    private void OnEnable()
    {
        if (_remainingLifetime <= 0f)
        {
            _remainingLifetime = Mathf.Max(0f, lifetimeSeconds);
        }
    }

    private void Update()
    {
        if (_isPaused || _remainingLifetime <= 0f)
        {
            return;
        }

        _remainingLifetime -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (_remainingLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void SetPaused(bool paused)
    {
        _isPaused = paused;
    }

    public void ResetLifetime(float? newLifetime = null, bool keepPausedState = true)
    {
        _remainingLifetime = Mathf.Max(0f, newLifetime ?? lifetimeSeconds);
        if (!keepPausedState)
        {
            _isPaused = false;
        }
    }

    private void OnValidate()
    {
        lifetimeSeconds = Mathf.Max(0f, lifetimeSeconds);
    }
}
