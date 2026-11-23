using UnityEngine;

public class Damager : MonoBehaviour
{
    [field: SerializeField] public float Damage { get; private set; } = 1f;

    private void OnValidate()
    {
        Damage = Mathf.Max(0f, Damage);
    }
}
