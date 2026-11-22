using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class MouseInputManager
{
    private static int _cachedFrame = -1;
    private static bool _hasMousePosition;
    private static Vector2 _mousePosition;
    private static bool _leftPressedThisFrame;
    private static bool _leftButtonHeld;

    public static bool TryGetMousePosition(out Vector2 mousePosition)
    {
        Refresh();
        mousePosition = _mousePosition;
        return _hasMousePosition;
    }

    public static bool GetLeftPressedThisFrame()
    {
        Refresh();
        return _leftPressedThisFrame;
    }

    public static bool GetLeftButtonHeld()
    {
        Refresh();
        return _leftButtonHeld;
    }

    public static bool IsMouseInsideGameView()
    {
        Refresh();
        if (!_hasMousePosition)
        {
            return false;
        }

        return _mousePosition.x >= 0f &&
               _mousePosition.x <= Screen.width &&
               _mousePosition.y >= 0f &&
               _mousePosition.y <= Screen.height;
    }

    private static void Refresh()
    {
        var currentFrame = Time.frameCount;
        if (_cachedFrame == currentFrame)
        {
            return;
        }

        _cachedFrame = currentFrame;
        _hasMousePosition = false;
        _mousePosition = default;
        _leftPressedThisFrame = false;
        _leftButtonHeld = false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            _mousePosition = Mouse.current.position.ReadValue();
            _leftPressedThisFrame = Mouse.current.leftButton.wasPressedThisFrame;
            _leftButtonHeld = Mouse.current.leftButton.isPressed;
            _hasMousePosition = true;
            return;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        _mousePosition = Input.mousePosition;
        _leftPressedThisFrame = Input.GetMouseButtonDown(0);
        _leftButtonHeld = Input.GetMouseButton(0);
        _hasMousePosition = true;
#endif
    }
}
