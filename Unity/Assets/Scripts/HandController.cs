using UnityEngine;
using UnityEngine.InputSystem;

namespace TheFrightBeforeChristmas
{
    public class HandController : MonoBehaviour
    {
        [Header("Target & Movement")]
        [Tooltip("The body/pivot transform around which the hand rotates. Defaults to parent.")]
        [SerializeField] private Transform pivot;

        [Tooltip("Half-extents (X and Y radii) of the movement path around the body. Auto-calculated from initial position or body size if left at 0.")]
        [SerializeField] private Vector2 radius = Vector2.zero;

        [Tooltip("Minimum normalized Y position relative to the pivot (-1 is bottom, 0 is center/hips, 1 is top).")]
        [SerializeField] private float minY = 0f;

        [Tooltip("Smoothing speed for following the mouse. 0 means instant.")]
        [SerializeField] private float smoothSpeed = 0f;

        [Header("Rotation")]
        [Tooltip("Whether the hand rotates to face the mouse position.")]
        [SerializeField] private bool rotateTowardsMouse = true;

        [Tooltip("Angular offset in degrees added to the rotation.")]
        [SerializeField] private float rotationOffset = 0f;

        [Tooltip("Flip the hand's Y local scale when on the left (X-) side of the body.")]
        [SerializeField] private bool flipYOnLeftSide = true;

        private float _baseScaleY;
        private Camera _targetCamera;
        private Camera TargetCamera => _targetCamera != null ? _targetCamera : (_targetCamera = Camera.main);

        private void Awake()
        {
            _baseScaleY = Mathf.Abs(transform.localScale.y);

            if (pivot == null && transform.parent != null)
            {
                pivot = transform.parent;
            }

            if (radius.x <= 0f || radius.y <= 0f)
            {
                if (pivot != null)
                {
                    // If the body has a collider or renderer, use its half-extents plus a margin
                    if (pivot.TryGetComponent<Collider>(out var col))
                    {
                        var extents = col.bounds.extents;
                        radius = new Vector2(extents.x + 0.3f, extents.y + 0.3f);
                    }
                    else if (pivot.TryGetComponent<Renderer>(out var rend))
                    {
                        var extents = rend.bounds.extents;
                        radius = new Vector2(extents.x + 0.3f, extents.y + 0.3f);
                    }
                    else
                    {
                        Vector3 offset = transform.position - pivot.position;
                        radius = new Vector2(
                            radius.x > 0f ? radius.x : Mathf.Max(Mathf.Abs(offset.x), 0.5f),
                            radius.y > 0f ? radius.y : Mathf.Max(Mathf.Abs(offset.y), 1.0f)
                        );
                    }
                }
                else
                {
                    radius = new Vector2(0.5f, 1.0f);
                }
            }
        }

        private void Update()
        {
            FollowMouse();
        }

        private void FollowMouse()
        {
            if (Mouse.current == null || TargetCamera == null) return;

            Vector3 pivotPosition = pivot != null ? pivot.position : Vector3.zero;

            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

            // Project mouse position onto the XY plane at the pivot's Z depth
            Ray ray = TargetCamera.ScreenPointToRay(mouseScreenPos);
            Plane xyPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, pivotPosition.z));

            if (!xyPlane.Raycast(ray, out float enter)) return;

            Vector3 mouseWorldPos = ray.GetPoint(enter);
            Vector2 direction = new Vector2(mouseWorldPos.x - pivotPosition.x, mouseWorldPos.y - pivotPosition.y);

            if (direction.sqrMagnitude < 0.0001f) return;

            direction.Normalize();

            // Calculate target position along the elliptical path on the XY plane around the pivot
            float angle = Mathf.Atan2(direction.y, direction.x);

            // Restrict from going below the hips (minY threshold, where 0 is pivot height)
            float minSin = Mathf.Clamp(minY, -1f, 1f);
            float minAngle = Mathf.Asin(minSin); // in [-pi/2, pi/2]
            float maxAngle = Mathf.PI - minAngle; // corresponding angle on negative X side

            // Allowed angle range for the upper arc is [minAngle, maxAngle].
            // If the angle falls below this range (i.e. into the bottom region below hips):
            if (angle < minAngle || angle > maxAngle)
            {
                // Determine whether mouse is on the right (x >= 0) or left (x < 0) side of the body
                angle = direction.x >= 0f ? minAngle : maxAngle;
            }

            Vector3 targetPosition = new Vector3(
                pivotPosition.x + Mathf.Cos(angle) * radius.x,
                pivotPosition.y + Mathf.Sin(angle) * radius.y,
                transform.position.z
            );

            if (smoothSpeed > 0f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
            }
            else
            {
                transform.position = targetPosition;
            }

            if (rotateTowardsMouse)
            {
                float rotAngle = angle * Mathf.Rad2Deg + rotationOffset;
                Quaternion targetRotation = Quaternion.Euler(0f, 0f, rotAngle);

                if (smoothSpeed > 0f)
                {
                    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
                }
                else
                {
                    transform.rotation = targetRotation;
                }
            }

            if (flipYOnLeftSide)
            {
                Vector3 currentScale = transform.localScale;
                float sign = (targetPosition.x < pivotPosition.x) ? -1f : 1f;
                currentScale.y = _baseScaleY * sign;
                transform.localScale = currentScale;
            }
        }
    }
}