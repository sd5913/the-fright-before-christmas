using System.Reflection;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class ConstraintWeightDriver : MonoBehaviour
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

    private void Update()
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

        _currentWeight = Mathf.MoveTowards(_currentWeight, targetWeight, blendSpeed * Time.deltaTime);
        WriteWeight(_currentWeight);
    }

    private bool IsMouseInsideGameView()
    {
        if (!TryGetMousePosition(out var mouse))
        {
            return false;
        }

        return mouse.x >= 0f && mouse.x <= Screen.width && mouse.y >= 0f && mouse.y <= Screen.height;
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

    private static bool TryGetMousePosition(out Vector2 mousePosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            mousePosition = Mouse.current.position.ReadValue();
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        mousePosition = Input.mousePosition;
        return true;
#else
        mousePosition = default;
        return false;
#endif
    }
}
