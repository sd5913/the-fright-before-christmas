using UnityEngine;

public class Health : MonoBehaviour
{
    public float CurrentHealth => currentHealth;

    [SerializeField] private float maxHealth = 3f;
    [SerializeField] private float currentHealth = 3f;
    [SerializeField] private float hitShakeIntensity = 1f;

    private void Awake()
    {
        maxHealth = Mathf.Max(0f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        var damager = other.GetComponent<Damager>() ?? other.GetComponentInParent<Damager>();
        if (damager == null || damager.gameObject == gameObject)
        {
            return;
        }

        Destroy(damager.gameObject);

        if (currentHealth <= 0f)
        {
            return;
        }

        ApplyDamage(damager.Damage);
    }

    public void ApplyDamage(float damage)
    {
        if (damage <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damage);

        if (CameraFeedback.Instance != null && hitShakeIntensity > 0f)
        {
            CameraFeedback.Instance.PlayDefaultShake(hitShakeIntensity);
        }

        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnValidate()
    {
        maxHealth = Mathf.Max(0f, maxHealth);
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        hitShakeIntensity = Mathf.Max(0f, hitShakeIntensity);
    }
}
