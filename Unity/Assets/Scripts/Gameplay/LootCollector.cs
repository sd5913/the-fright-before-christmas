using UnityEngine;

public class LootCollector : CharacterBehaviour
{
    [SerializeField] private float collectRange = 3f;
    [SerializeField] private float collectConeAngle = 45f;
    [SerializeField] private float vacuumLerpSpeed = 14f;
    [SerializeField] private float rotationLerpSpeed = 16f;
    [SerializeField] private float shrinkLerpSpeed = 10f;
    [SerializeField] private float maxCollectDuration = 0.5f;
    [SerializeField] private bool onlyWhenFocused = true;
    private readonly System.Collections.Generic.List<LootItem> _lootBuffer = new System.Collections.Generic.List<LootItem>(64);

    protected override void OnUpdate(float deltaTime)
    {
        if (onlyWhenFocused && (gameCharacter == null || !gameCharacter.IsFocused))
        {
            return;
        }

        var forwardXY = new Vector2(transform.forward.x, transform.forward.y).normalized;
        if (forwardXY.sqrMagnitude <= 0.000001f)
        {
            forwardXY = Vector2.up;
        }

        var originXY = new Vector2(transform.position.x, transform.position.y);
        var halfAngleRadians = collectConeAngle * 0.5f * Mathf.Deg2Rad;
        var coneRadiusScale = Mathf.Tan(halfAngleRadians);

        _lootBuffer.Clear();
        foreach (var lootItem in LootItem.ActiveItems)
        {
            if (lootItem != null)
            {
                _lootBuffer.Add(lootItem);
            }
        }

        for (var i = 0; i < _lootBuffer.Count; i++)
        {
            var lootItem = _lootBuffer[i];
            var lootPos = lootItem.transform.position;
            var toLootXY = new Vector2(lootPos.x, lootPos.y) - originXY;
            var axialDistance = Vector2.Dot(forwardXY, toLootXY);
            if (axialDistance < 0f || axialDistance > collectRange)
            {
                continue;
            }

            if (axialDistance <= Mathf.Epsilon)
            {
                lootItem.BeginCollect(transform, vacuumLerpSpeed, rotationLerpSpeed, shrinkLerpSpeed, maxCollectDuration);
                continue;
            }

            var radialVector = toLootXY - forwardXY * axialDistance;
            var radialDistance = radialVector.magnitude;
            var maxRadiusAtDistance = axialDistance * coneRadiusScale;
            if (radialDistance > maxRadiusAtDistance)
            {
                continue;
            }

            lootItem.BeginCollect(transform, vacuumLerpSpeed, rotationLerpSpeed, shrinkLerpSpeed, maxCollectDuration);
        }
    }

    private void OnValidate()
    {
        collectRange = Mathf.Max(0.01f, collectRange);
        collectConeAngle = Mathf.Clamp(collectConeAngle, 1f, 179f);
        vacuumLerpSpeed = Mathf.Max(0.01f, vacuumLerpSpeed);
        rotationLerpSpeed = Mathf.Max(0.01f, rotationLerpSpeed);
        shrinkLerpSpeed = Mathf.Max(0.01f, shrinkLerpSpeed);
        maxCollectDuration = Mathf.Max(0.05f, maxCollectDuration);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.1f, 1f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, collectRange);

        var half = collectConeAngle * 0.5f;
        var left = Quaternion.AngleAxis(-half, transform.up) * transform.forward;
        var right = Quaternion.AngleAxis(half, transform.up) * transform.forward;
        Gizmos.DrawRay(transform.position, left * collectRange);
        Gizmos.DrawRay(transform.position, right * collectRange);
    }
}
