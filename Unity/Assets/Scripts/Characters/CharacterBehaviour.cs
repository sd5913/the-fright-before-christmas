using UnityEngine;

public enum CharacterUpdatePhase
{
    Update,
    LateUpdate
}

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

    internal CharacterUpdatePhase UpdatePhase => GetUpdatePhase();

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

    protected virtual CharacterUpdatePhase GetUpdatePhase()
    {
        return CharacterUpdatePhase.Update;
    }

    protected abstract void OnUpdate(float deltaTime);
}
