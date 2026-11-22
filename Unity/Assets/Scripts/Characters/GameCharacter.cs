using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

[DisallowMultipleComponent]
public class GameCharacter : MonoBehaviour
{
    private readonly List<CharacterBehaviour> _behaviours = new List<CharacterBehaviour>();
    private readonly List<Rig> _rigs = new List<Rig>();
    private bool _isReady;
    private float _targetRigWeight;

    public bool IsFocused;
    [SerializeField] private float rigFocusBlendSpeed = 8f;

    private void Awake()
    {
        RefreshBehaviours();
        RefreshRigs();
        _targetRigWeight = IsFocused ? 1f : 0f;
        ApplyFocusToRigs(_targetRigWeight, true);
    }

    private void Start()
    {
        _isReady = true;
    }

    private void OnValidate()
    {
        rigFocusBlendSpeed = Mathf.Max(0f, rigFocusBlendSpeed);

        if (Application.isPlaying)
        {
            _targetRigWeight = IsFocused ? 1f : 0f;
            return;
        }

        RefreshBehaviours();
        RefreshRigs();
        _targetRigWeight = IsFocused ? 1f : 0f;
        ApplyFocusToRigs(_targetRigWeight, true);
    }

    private void OnTransformChildrenChanged()
    {
        RefreshBehaviours();
        RefreshRigs();
        _targetRigWeight = IsFocused ? 1f : 0f;
        ApplyFocusToRigs(_targetRigWeight, true);
    }

    private void Update()
    {
        if (!_isReady)
        {
            return;
        }

        _targetRigWeight = IsFocused ? 1f : 0f;

        UpdateRigWeights(Time.deltaTime);

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

    [ContextMenu("Refresh Rigs")]
    public void RefreshRigs()
    {
        _rigs.Clear();
        var found = GetComponentsInChildren<Rig>(true);
        for (var i = 0; i < found.Length; i++)
        {
            if (found[i] != null)
            {
                _rigs.Add(found[i]);
            }
        }
    }

    private void UpdateRigWeights(float deltaTime)
    {
        for (var i = 0; i < _rigs.Count; i++)
        {
            var rig = _rigs[i];
            if (rig == null)
            {
                continue;
            }

            if (rigFocusBlendSpeed <= 0f)
            {
                rig.weight = _targetRigWeight;
            }
            else
            {
                rig.weight = Mathf.MoveTowards(rig.weight, _targetRigWeight, rigFocusBlendSpeed * deltaTime);
            }
        }
    }

    private void ApplyFocusToRigs(float targetWeight, bool force = false)
    {
        for (var i = 0; i < _rigs.Count; i++)
        {
            var rig = _rigs[i];
            if (rig == null)
            {
                continue;
            }

            if (force || !Mathf.Approximately(rig.weight, targetWeight))
            {
                rig.weight = targetWeight;
            }
        }
    }

}
