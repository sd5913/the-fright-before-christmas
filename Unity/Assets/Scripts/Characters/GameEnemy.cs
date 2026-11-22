using System.Collections;
using UnityEngine;

public class GameEnemy : MonoBehaviour
{
    [SerializeField] private Transform iceCube;
    [SerializeField] private Animator animator;
    [SerializeField] private float freezeRampDuration = 0.25f;
    [SerializeField] private float unfreezeRampDuration = 0.25f;
    [SerializeField] private float frozenAnimatorSpeed = 0f;
    [SerializeField] private float normalAnimatorSpeed = 1f;
    [SerializeField] private float icePunchScaleMultiplier = 1.15f;
    [SerializeField] private float icePunchDuration = 0.18f;

    private Coroutine _animSpeedRoutine;
    private Coroutine _icePunchRoutine;
    private Vector3 _iceBaseScale = Vector3.one;

    private void Awake()
    {
        if (iceCube != null)
        {
            _iceBaseScale = iceCube.localScale;
        }
    }
    
    [DarkButton]
    public void Freeze()
    {
        if (iceCube != null)
        {
            if (!iceCube.gameObject.activeSelf)
            {
                iceCube.localScale = _iceBaseScale;
            }

            iceCube.gameObject.SetActive(true);
            StartIcePunch(deactivateAtEnd: false);
        }

        StartAnimatorSpeedRamp(frozenAnimatorSpeed, freezeRampDuration);
    }

    [DarkButton]
    public void Unfreeze()
    {
        StartAnimatorSpeedRamp(normalAnimatorSpeed, unfreezeRampDuration);

        if (iceCube != null && iceCube.gameObject.activeSelf)
        {
            StartIcePunch(deactivateAtEnd: true);
        }
    }

    private void StartAnimatorSpeedRamp(float targetSpeed, float duration)
    {
        if (animator == null)
        {
            return;
        }

        if (_animSpeedRoutine != null)
        {
            StopCoroutine(_animSpeedRoutine);
        }

        _animSpeedRoutine = StartCoroutine(AnimateAnimatorSpeed(targetSpeed, duration));
    }

    private IEnumerator AnimateAnimatorSpeed(float targetSpeed, float duration)
    {
        var startSpeed = animator.speed;
        var elapsed = 0f;
        var clampedDuration = Mathf.Max(0f, duration);

        if (clampedDuration <= Mathf.Epsilon)
        {
            animator.speed = targetSpeed;
            _animSpeedRoutine = null;
            yield break;
        }

        while (elapsed < clampedDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / clampedDuration);
            t = t * t * (3f - 2f * t);
            animator.speed = Mathf.Lerp(startSpeed, targetSpeed, t);
            yield return null;
        }

        animator.speed = targetSpeed;
        _animSpeedRoutine = null;
    }

    private void StartIcePunch(bool deactivateAtEnd)
    {
        if (_icePunchRoutine != null)
        {
            StopCoroutine(_icePunchRoutine);
        }

        _icePunchRoutine = StartCoroutine(AnimateIcePunch(deactivateAtEnd));
    }

    private IEnumerator AnimateIcePunch(bool deactivateAtEnd)
    {
        if (iceCube == null)
        {
            _icePunchRoutine = null;
            yield break;
        }

        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, icePunchDuration);
        var peakScale = _iceBaseScale * Mathf.Max(1f, icePunchScaleMultiplier);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var pulse = t < 0.5f
                ? Mathf.Lerp(0f, 1f, t * 2f)
                : Mathf.Lerp(1f, 0f, (t - 0.5f) * 2f);
            iceCube.localScale = Vector3.Lerp(_iceBaseScale, peakScale, pulse);
            yield return null;
        }

        iceCube.localScale = _iceBaseScale;

        if (deactivateAtEnd)
        {
            iceCube.gameObject.SetActive(false);
        }

        _icePunchRoutine = null;
    }

    private void OnDisable()
    {
        if (_animSpeedRoutine != null)
        {
            StopCoroutine(_animSpeedRoutine);
            _animSpeedRoutine = null;
        }

        if (_icePunchRoutine != null)
        {
            StopCoroutine(_icePunchRoutine);
            _icePunchRoutine = null;
        }
    }
}