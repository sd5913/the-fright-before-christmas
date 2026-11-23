using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup m_cg;
    [SerializeField] private Transform m_punchTarget;
    [SerializeField] private float m_punchAmount = 0.18f;
    [SerializeField] private float m_punchDuration = 0.2f;
    [SerializeField] private int m_punchVibrato = 2;

    private Coroutine _punchRoutine;
    private Vector3 _baseScale = Vector3.one;

    private void Awake()
    {
        var target = m_punchTarget != null ? m_punchTarget : transform;
        _baseScale = target.localScale;
    }

    private void OnEnable()
    {
        if (_punchRoutine != null)
        {
            StopCoroutine(_punchRoutine);
        }

        _punchRoutine = StartCoroutine(PunchScaleRoutine());
    }

    private void OnDisable()
    {
        if (_punchRoutine != null)
        {
            StopCoroutine(_punchRoutine);
            _punchRoutine = null;
        }

        var target = m_punchTarget != null ? m_punchTarget : transform;
        target.localScale = _baseScale;
    }

    public void Restart()
    {
        var activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.buildIndex);
    }

    private IEnumerator PunchScaleRoutine()
    {
        var target = m_punchTarget != null ? m_punchTarget : transform;
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, m_punchDuration);
        var amount = Mathf.Max(0f, m_punchAmount);
        var vibrato = Mathf.Max(1, m_punchVibrato);

        target.localScale = _baseScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var wave = Mathf.Sin(t * Mathf.PI * vibrato);
            var decay = 1f - t;
            var multiplier = 1f + wave * decay * amount;
            target.localScale = _baseScale * multiplier;
            yield return null;
        }

        target.localScale = _baseScale;
        _punchRoutine = null;
    }

    private void OnValidate()
    {
        m_punchAmount = Mathf.Max(0f, m_punchAmount);
        m_punchDuration = Mathf.Max(0.01f, m_punchDuration);
        m_punchVibrato = Mathf.Max(1, m_punchVibrato);
    }
}
