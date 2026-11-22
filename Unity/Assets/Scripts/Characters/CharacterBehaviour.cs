using UnityEngine;

public abstract class CharacterBehaviour : MonoBehaviour
{
    private GameCharacter _gameCharacter;

    public GameCharacter gameCharacter
    {
        get
        {
            if (_gameCharacter == null)
            {
                _gameCharacter = GetComponentInParent<GameCharacter>();
            }

            return _gameCharacter;
        }
    }

    protected GameCharacter GameCharacter => gameCharacter;

    internal void BindGameCharacter(GameCharacter owner)
    {
        _gameCharacter = owner;
    }

    internal void ExecuteUpdate(float deltaTime)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        OnUpdate(deltaTime);
    }

    internal void ExecuteLateUpdate(float deltaTime)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        OnLateUpdate(deltaTime);
    }

    protected virtual void OnUpdate(float deltaTime)
    {
    }

    protected virtual void OnLateUpdate(float deltaTime)
    {
    }
}
