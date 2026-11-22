using UnityEngine;

public enum CharacterUpdatePhase
{
    Update,
    LateUpdate
}

public abstract class CharacterBehaviour : MonoBehaviour
{
    internal CharacterUpdatePhase UpdatePhase => GetUpdatePhase();

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
