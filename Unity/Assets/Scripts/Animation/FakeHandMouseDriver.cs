using UnityEngine;

[DisallowMultipleComponent]
public class FakeHandMouseDriver : CharacterBehaviour
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

    [Header("Local Limits")]
    [SerializeField] private bool clampMinLocalY = true;
    [SerializeField] private float minLocalY;

    [Header("Rotation")]
    [SerializeField] private bool rotateToMouse = true;
    [SerializeField] private float rotationSmoothSpeed = 16f;
    [SerializeField] private Vector3 rotationEulerOffset;

    private Vector3 _velocity;

    protected override void OnLateUpdate(float deltaTime)
    {
        if (characterRoot == null || handTransform == null)
        {
            return;
        }

        var activeCamera = sourceCamera != null ? sourceCamera : Camera.main;
        if (activeCamera == null || !MouseInputManager.TryGetMousePosition(out var mousePosition))
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
        var depthScale = 1f;
        if (Mathf.Abs(cameraForwardOffset) > 0f)
        {
            var depth = anchorScreen.z;
            var offsetDepth = Mathf.Max(0.001f, depth + cameraForwardOffset);
            depthScale = offsetDepth / Mathf.Max(depth, 0.001f);
        }

        var targetPosition =
            anchorPosition +
            activeCamera.transform.right * (clamped.x * depthScale) +
            activeCamera.transform.up * (clamped.y * depthScale) +
            activeCamera.transform.forward * cameraForwardOffset;
        if (clampMinLocalY)
        {
            var localTarget = characterRoot.InverseTransformPoint(targetPosition);
            localTarget.y = Mathf.Max(localTarget.y, minLocalY);
            targetPosition = characterRoot.TransformPoint(localTarget);
        }

        if (smoothTime <= 0f)
        {
            handTransform.position = targetPosition;
            _velocity = Vector3.zero;
        }
        else
        {
            handTransform.position = Vector3.SmoothDamp(
                handTransform.position,
                targetPosition,
                ref _velocity,
                smoothTime);
        }

        if (rotateToMouse)
        {
            RotateTowardsMouse(mouseOnCharacterPlane, activeCamera, deltaTime);
        }
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
        rotationSmoothSpeed = Mathf.Max(0f, rotationSmoothSpeed);
    }

    private void RotateTowardsMouse(Vector3 mouseOnCharacterPlane, Camera activeCamera, float deltaTime)
    {
        var aimDirection = mouseOnCharacterPlane - handTransform.position;
        if (aimDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        var desiredRotation = Quaternion.LookRotation(aimDirection.normalized, activeCamera.transform.up) *
                              Quaternion.Euler(rotationEulerOffset);
        if (rotationSmoothSpeed <= 0f)
        {
            handTransform.rotation = desiredRotation;
            return;
        }

        var t = 1f - Mathf.Exp(-rotationSmoothSpeed * deltaTime);
        handTransform.rotation = Quaternion.Slerp(handTransform.rotation, desiredRotation, t);
    }

}
