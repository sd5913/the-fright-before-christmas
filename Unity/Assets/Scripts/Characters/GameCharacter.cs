using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class GameCharacter : MonoBehaviour
{
    private readonly List<CharacterBehaviour> _behaviours = new List<CharacterBehaviour>();
    private bool _isReady;

    public bool IsFocused;

    private void Awake()
    {
        RefreshBehaviours();
    }

    private void Start()
    {
        _isReady = true;
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
        if (!_isReady)
        {
            return;
        }

        for (var i = 0; i < _behaviours.Count; i++)
        {
            var behaviour = _behaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            behaviour.ExecuteUpdate(Time.deltaTime);
        }
    }

    private void LateUpdate()
    {
        if (!_isReady)
        {
            return;
        }

        for (var i = 0; i < _behaviours.Count; i++)
        {
            var behaviour = _behaviours[i];
            if (behaviour == null)
            {
                continue;
            }

            behaviour.ExecuteLateUpdate(Time.deltaTime);
        }
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

}
