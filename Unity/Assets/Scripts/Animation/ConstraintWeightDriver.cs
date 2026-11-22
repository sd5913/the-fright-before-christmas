using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class ConstraintWeightDriver : CharacterBehaviour
{
    [SerializeField] private Component weightedComponent;
    [SerializeField] private string weightMemberName = "weight";
    [SerializeField] private bool disableWhenMouseOutsideGameView = true;
    [SerializeField] private float blendSpeed = 8f;
    [SerializeField, Range(0f, 1f)] private float maxWeight = 1f;

    private MemberInfo _weightMember;
    private float _currentWeight;

    private void Awake()
    {
        ResolveWeightMember();
        _currentWeight = ReadWeight();
    }

    protected override void OnUpdate(float deltaTime)
    {
        if (_weightMember == null)
        {
            return;
        }

        var targetWeight = maxWeight;
        if (disableWhenMouseOutsideGameView && !IsMouseInsideGameView())
        {
            targetWeight = 0f;
        }

        _currentWeight = Mathf.MoveTowards(_currentWeight, targetWeight, blendSpeed * deltaTime);
        WriteWeight(_currentWeight);
    }

    private bool IsMouseInsideGameView()
    {
        return MouseInputManager.IsMouseInsideGameView();
    }

    private void ResolveWeightMember()
    {
        _weightMember = null;
        if (weightedComponent == null || string.IsNullOrWhiteSpace(weightMemberName))
        {
            return;
        }

        var type = weightedComponent.GetType();
        _weightMember =
            (MemberInfo)type.GetProperty(weightMemberName, BindingFlags.Instance | BindingFlags.Public) ??
            type.GetField(weightMemberName, BindingFlags.Instance | BindingFlags.Public);
    }

    private float ReadWeight()
    {
        if (weightedComponent == null || _weightMember == null)
        {
            return 0f;
        }

        if (_weightMember is PropertyInfo propertyInfo && propertyInfo.CanRead && propertyInfo.PropertyType == typeof(float))
        {
            return (float)propertyInfo.GetValue(weightedComponent);
        }

        if (_weightMember is FieldInfo fieldInfo && fieldInfo.FieldType == typeof(float))
        {
            return (float)fieldInfo.GetValue(weightedComponent);
        }

        return 0f;
    }

    private void WriteWeight(float value)
    {
        if (weightedComponent == null || _weightMember == null)
        {
            return;
        }

        if (_weightMember is PropertyInfo propertyInfo && propertyInfo.CanWrite && propertyInfo.PropertyType == typeof(float))
        {
            propertyInfo.SetValue(weightedComponent, value);
        }
        else if (_weightMember is FieldInfo fieldInfo && fieldInfo.FieldType == typeof(float))
        {
            fieldInfo.SetValue(weightedComponent, value);
        }
    }

    private void OnValidate()
    {
        maxWeight = Mathf.Clamp01(maxWeight);
        blendSpeed = Mathf.Max(0f, blendSpeed);
        ResolveWeightMember();
    }
}
