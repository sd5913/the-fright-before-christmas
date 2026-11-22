using UnityEngine;

[DisallowMultipleComponent]
public class MouseHeadLookTargetDriver : CharacterBehaviour
{
    private enum TargetingMode
    {
        ScreenSpaceFrontFacing,
        WorldRaycast
    }

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
    [SerializeField] private TargetingMode targetingMode = TargetingMode.ScreenSpaceFrontFacing;
    [SerializeField] private float horizontalScreenRange = 0.45f;
    [SerializeField] private float verticalScreenRange = 0.45f;
    [SerializeField] private bool invertHorizontal = true;
    [SerializeField] private bool raycastToWorld = true;
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private float raycastMaxDistance = 100f;
    [SerializeField] private float fallbackPlaneForwardOffset = 2f;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.06f;

    private Vector3 _targetVelocity;

    protected override void OnUpdate(float deltaTime)
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

        Vector3 clampedTargetPoint;
        var isFocused = gameCharacter != null && gameCharacter.IsFocused;
        if (!isFocused)
        {
            clampedTargetPoint = BuildNeutralTargetPoint();
        }
        else
        {
            if (!MouseInputManager.TryGetMousePosition(out var mousePosition))
            {
                clampedTargetPoint = BuildNeutralTargetPoint();
            }
            else if (targetingMode == TargetingMode.ScreenSpaceFrontFacing)
            {
                clampedTargetPoint = BuildFrontFacingTargetPoint(activeCamera, mousePosition);
            }
            else
            {
                var mouseRay = activeCamera.ScreenPointToRay(mousePosition);
                var desiredPoint = ResolveDesiredLookPoint(mouseRay);
                clampedTargetPoint = BuildClampedTargetPoint(desiredPoint);
            }
        }

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

    private Vector3 BuildFrontFacingTargetPoint(Camera activeCamera, Vector2 mousePosition)
    {
        var viewport = activeCamera.ScreenToViewportPoint(mousePosition);
        var horizontal = Mathf.Clamp((viewport.x - 0.5f) / Mathf.Max(0.01f, horizontalScreenRange), -1f, 1f);
        var vertical = Mathf.Clamp((viewport.y - 0.5f) / Mathf.Max(0.01f, verticalScreenRange), -1f, 1f);
        if (invertHorizontal)
        {
            horizontal = -horizontal;
        }

        var yaw = horizontal >= 0f
            ? Mathf.Lerp(0f, yawLimits.y, horizontal)
            : Mathf.Lerp(0f, yawLimits.x, -horizontal);

        var pitch = vertical >= 0f
            ? Mathf.Lerp(0f, pitchLimits.x, vertical)
            : Mathf.Lerp(0f, pitchLimits.y, -vertical);

        var localRotation = Quaternion.Euler(pitch, yaw, 0f);
        var worldDirection = characterRoot.TransformDirection(localRotation * Vector3.forward);
        return headPivot.position + worldDirection * Mathf.Max(0.1f, lookDistance);
    }

    private Vector3 BuildNeutralTargetPoint()
    {
        return headPivot.position + characterRoot.forward * Mathf.Max(0.1f, lookDistance);
    }

    private void OnValidate()
    {
        horizontalScreenRange = Mathf.Clamp(horizontalScreenRange, 0.01f, 0.5f);
        verticalScreenRange = Mathf.Clamp(verticalScreenRange, 0.01f, 0.5f);
        raycastMaxDistance = Mathf.Max(0.1f, raycastMaxDistance);
        fallbackPlaneForwardOffset = Mathf.Max(0.1f, fallbackPlaneForwardOffset);
        lookDistance = Mathf.Max(0.1f, lookDistance);
        smoothTime = Mathf.Max(0f, smoothTime);
    }
}
