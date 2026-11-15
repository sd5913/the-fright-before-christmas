using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class MouseHeadLookTargetDriver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private Transform headPivot;
    [SerializeField] private Transform lookTarget;

    [Header("Look Limits")]
    [SerializeField] private Vector2 yawLimits = new Vector2(-60f, 60f);
    [SerializeField] private Vector2 pitchLimits = new Vector2(-30f, 35f);
    [SerializeField] private float lookDistance = 2f;

    [Header("Targeting")]
    [SerializeField] private bool raycastToWorld = true;
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private float raycastMaxDistance = 100f;
    [SerializeField] private float fallbackPlaneForwardOffset = 2f;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.06f;

    private Vector3 _targetVelocity;

    private void Update()
    {
        if (characterRoot == null || headPivot == null || lookTarget == null)
        {
            return;
        }

        var activeCamera = sourceCamera != null ? sourceCamera : Camera.main;
        if (activeCamera == null)
        {
            return;
        }

        if (!TryGetMousePosition(out var mousePosition))
        {
            return;
        }

        var mouseRay = activeCamera.ScreenPointToRay(mousePosition);
        var desiredPoint = ResolveDesiredLookPoint(mouseRay);
        var clampedTargetPoint = BuildClampedTargetPoint(desiredPoint);

        if (smoothTime <= 0f)
        {
            lookTarget.position = clampedTargetPoint;
            _targetVelocity = Vector3.zero;
            return;
        }

        lookTarget.position = Vector3.SmoothDamp(
            lookTarget.position,
            clampedTargetPoint,
            ref _targetVelocity,
            smoothTime);
    }

    private Vector3 ResolveDesiredLookPoint(Ray mouseRay)
    {
        if (raycastToWorld &&
            Physics.Raycast(
                mouseRay,
                out var hit,
                raycastMaxDistance,
                raycastMask,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }

        var planeOrigin = characterRoot.position + characterRoot.forward * fallbackPlaneForwardOffset;
        var lookPlane = new Plane(characterRoot.forward, planeOrigin);
        if (lookPlane.Raycast(mouseRay, out var enter))
        {
            return mouseRay.GetPoint(enter);
        }

        return headPivot.position + characterRoot.forward * lookDistance;
    }

    private Vector3 BuildClampedTargetPoint(Vector3 worldPoint)
    {
        var desiredDirection = (worldPoint - headPivot.position).normalized;
        if (desiredDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            desiredDirection = characterRoot.forward;
        }

        var localDirection = characterRoot.InverseTransformDirection(desiredDirection);

        var yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
        var pitch = -Mathf.Asin(Mathf.Clamp(localDirection.y, -1f, 1f)) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(yaw, yawLimits.x, yawLimits.y);
        pitch = Mathf.Clamp(pitch, pitchLimits.x, pitchLimits.y);

        var localRotation = Quaternion.Euler(pitch, yaw, 0f);
        var clampedWorldDirection = characterRoot.TransformDirection(localRotation * Vector3.forward);

        return headPivot.position + clampedWorldDirection * Mathf.Max(0.1f, lookDistance);
    }

    private void OnValidate()
    {
        raycastMaxDistance = Mathf.Max(0.1f, raycastMaxDistance);
        fallbackPlaneForwardOffset = Mathf.Max(0.1f, fallbackPlaneForwardOffset);
        lookDistance = Mathf.Max(0.1f, lookDistance);
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
