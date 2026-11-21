using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class FakeHandMouseDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform characterRoot;
    [SerializeField] private Transform handTransform;
    [SerializeField] private Camera sourceCamera;

    [Header("Anchor")]
    [SerializeField] private Vector3 localAnchorOffset = new Vector3(0f, 1.25f, 0f);
    [SerializeField] private float cameraForwardOffset;

    [Header("Capsule Bounds (Camera Plane)")]
    [SerializeField] private float capsuleRadius = 0.45f;
    [SerializeField] private float capsuleHalfHeight = 0.4f;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.04f;

    private Vector3 _velocity;

    private void LateUpdate()
    {
        if (characterRoot == null || handTransform == null)
        {
            return;
        }

        var activeCamera = sourceCamera != null ? sourceCamera : Camera.main;
        if (activeCamera == null || !TryGetMousePosition(out var mousePosition))
        {
            return;
        }

        var anchorPosition = characterRoot.TransformPoint(localAnchorOffset);
        var anchorScreen = activeCamera.WorldToScreenPoint(anchorPosition);
        if (anchorScreen.z <= 0f)
        {
            return;
        }

        var mouseOnCharacterPlane = activeCamera.ScreenToWorldPoint(
            new Vector3(mousePosition.x, mousePosition.y, anchorScreen.z));

        var delta = mouseOnCharacterPlane - anchorPosition;
        var x = Vector3.Dot(delta, activeCamera.transform.right);
        var y = Vector3.Dot(delta, activeCamera.transform.up);

        var clamped = ClampToVerticalCapsule(x, y, capsuleRadius, capsuleHalfHeight);
        var targetPosition =
            anchorPosition +
            activeCamera.transform.right * clamped.x +
            activeCamera.transform.up * clamped.y +
            activeCamera.transform.forward * cameraForwardOffset;

        if (smoothTime <= 0f)
        {
            handTransform.position = targetPosition;
            _velocity = Vector3.zero;
            return;
        }

        handTransform.position = Vector3.SmoothDamp(
            handTransform.position,
            targetPosition,
            ref _velocity,
            smoothTime);
    }

    private static Vector2 ClampToVerticalCapsule(float x, float y, float radius, float halfHeight)
    {
        if (radius <= 0f)
        {
            return new Vector2(0f, Mathf.Clamp(y, -halfHeight, halfHeight));
        }

        if (halfHeight <= 0f)
        {
            var circle = new Vector2(x, y);
            return circle.sqrMagnitude > radius * radius ? circle.normalized * radius : circle;
        }

        if (y > halfHeight)
        {
            var top = new Vector2(x, y - halfHeight);
            if (top.sqrMagnitude > radius * radius)
            {
                top = top.normalized * radius;
            }

            return new Vector2(top.x, top.y + halfHeight);
        }

        if (y < -halfHeight)
        {
            var bottom = new Vector2(x, y + halfHeight);
            if (bottom.sqrMagnitude > radius * radius)
            {
                bottom = bottom.normalized * radius;
            }

            return new Vector2(bottom.x, bottom.y - halfHeight);
        }

        return new Vector2(Mathf.Clamp(x, -radius, radius), y);
    }

    private void OnValidate()
    {
        capsuleRadius = Mathf.Max(0f, capsuleRadius);
        capsuleHalfHeight = Mathf.Max(0f, capsuleHalfHeight);
        smoothTime = Mathf.Max(0f, smoothTime);
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
