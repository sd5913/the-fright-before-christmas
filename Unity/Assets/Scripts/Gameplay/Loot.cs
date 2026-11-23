using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Loot : MonoBehaviour
{
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private float punchAmount = 0.2f;
    [SerializeField] private float punchDuration = 0.2f;
    [SerializeField] private int punchVibrato = 2;

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (_health == null)
        {
            _health = GetComponent<Health>();
        }

        if (_health != null)
        {
            _health.onDeath += SpawnLoot;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.onDeath -= SpawnLoot;
        }
    }

    private void SpawnLoot()
    {
        if (prefabToSpawn == null)
        {
            return;
        }

        var spawned = Instantiate(prefabToSpawn, transform.position, prefabToSpawn.transform.rotation);

        var punch = spawned.GetComponent<LootScalePunch>();
        if (punch == null)
        {
            punch = spawned.AddComponent<LootScalePunch>();
        }

        punch.Play(punchAmount, punchDuration, punchVibrato);
    }

    private void OnValidate()
    {
        punchAmount = Mathf.Max(0f, punchAmount);
        punchDuration = Mathf.Max(0.01f, punchDuration);
        punchVibrato = Mathf.Max(1, punchVibrato);
    }
}

public class LootScalePunch : MonoBehaviour
{
    private Coroutine _routine;

    public void Play(float punchAmount, float punchDuration, int punchVibrato)
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _routine = StartCoroutine(PunchScaleRoutine(
            Mathf.Max(0f, punchAmount),
            Mathf.Max(0.01f, punchDuration),
            Mathf.Max(1, punchVibrato)));
    }

    private IEnumerator PunchScaleRoutine(float amount, float duration, int vibrato)
    {
        var baseScale = transform.localScale;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var wave = Mathf.Sin(t * Mathf.PI * vibrato);
            var decay = 1f - t;
            var multiplier = 1f + wave * decay * amount;
            transform.localScale = baseScale * multiplier;
            yield return null;
        }

        transform.localScale = baseScale;
        _routine = null;
    }
}
