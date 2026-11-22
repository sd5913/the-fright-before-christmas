using UnityEngine;

public class GameEnemy : MonoBehaviour
{
    [SerializeField] private Freezable freezable;
    [SerializeField] private CharacterSwapper characterSwapper;
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float targetRefreshInterval = 0.5f;
    [SerializeField] private bool useInitialZPosition = true;
    [SerializeField] private float fixedZPosition;

    private GameCharacter[] _characters = new GameCharacter[0];
    private float _nextTargetRefreshTime;

    private void Awake()
    {
        if (useInitialZPosition)
        {
            fixedZPosition = transform.position.z;
        }
    }

    private void Update()
    {
        if (freezable != null && freezable.IsFreezed)
        {
            KeepFixedZ();
            return;
        }

        RefreshTargetsIfNeeded();
        if (_characters == null || _characters.Length == 0)
        {
            KeepFixedZ();
            return;
        }

        var center = GetCharacterCenter();
        var current = transform.position;
        var movedXY = Vector2.MoveTowards(
            new Vector2(current.x, current.y),
            new Vector2(center.x, center.y),
            moveSpeed * Time.deltaTime);

        transform.position = new Vector3(movedXY.x, movedXY.y, fixedZPosition);
    }

    private void RefreshTargetsIfNeeded()
    {
        if (Time.time < _nextTargetRefreshTime && _characters != null && _characters.Length > 0)
        {
            return;
        }

        if (characterSwapper == null)
        {
            characterSwapper = FindAnyObjectByType<CharacterSwapper>();
        }

        _characters = characterSwapper != null
            ? characterSwapper.GetComponentsInChildren<GameCharacter>()
            : FindObjectsOfType<GameCharacter>();

        _nextTargetRefreshTime = Time.time + Mathf.Max(0.01f, targetRefreshInterval);
    }

    private Vector3 GetCharacterCenter()
    {
        var sum = Vector3.zero;
        var count = 0;

        for (var i = 0; i < _characters.Length; i++)
        {
            if (_characters[i] == null)
            {
                continue;
            }

            sum += _characters[i].transform.position;
            count++;
        }

        return count > 0 ? sum / count : transform.position;
    }

    private void KeepFixedZ()
    {
        var position = transform.position;
        if (Mathf.Approximately(position.z, fixedZPosition))
        {
            return;
        }

        position.z = fixedZPosition;
        transform.position = position;
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0f, moveSpeed);
        targetRefreshInterval = Mathf.Max(0.01f, targetRefreshInterval);
    }
}