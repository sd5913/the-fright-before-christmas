using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameCharacter : MonoBehaviour
{
    private readonly List<CharacterBehaviour> _behaviours = new List<CharacterBehaviour>();

    private void Awake()
    {
        RefreshBehaviours();
    }

    private void OnValidate()
    {
        RefreshBehaviours();
    }

    private void OnTransformChildrenChanged()
    {
        RefreshBehaviours();
    }

    private void Update()
    {
        RunBehaviours(CharacterUpdatePhase.Update, Time.deltaTime);
    }

    private void LateUpdate()
    {
        RunBehaviours(CharacterUpdatePhase.LateUpdate, Time.deltaTime);
    }

    [ContextMenu("Refresh Character Behaviours")]
    public void RefreshBehaviours()
    {
        _behaviours.Clear();
        var found = GetComponentsInChildren<CharacterBehaviour>(true);
        for (var i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
            {
                found[i].BindGameCharacter(this);
                _behaviours.Add(found[i]);
            }
        }
    }

    private void RunBehaviours(CharacterUpdatePhase phase, float deltaTime)
    {
        for (var i = 0; i < _behaviours.Count; i++)
        {
            var behaviour = _behaviours[i];
            if (behaviour == null || behaviour.UpdatePhase != phase)
            {
                continue;
            }

            behaviour.ExecuteUpdate(deltaTime);
        }
    }
}
