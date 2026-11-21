using UnityEngine;

[DisallowMultipleComponent]
public class ThrownProjectile : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float gravityMultiplier = 1f;
    [SerializeField] private bool alignToVelocity = true;

    [Header("Spin")]
    [SerializeField] private bool applySpin = true;
    [SerializeField] private Vector3 spinDegreesPerSecond = new Vector3(0f, 0f, 420f);

    [Header("Lifetime")]
    [SerializeField] private float destroyAfterSeconds = 6f;

    private Rigidbody _rigidbody;
    private Vector3 _velocity;
    private bool _isLaunched;
    private float _destroyAt;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        if (destroyAfterSeconds > 0f)
        {
            _destroyAt = Time.time + destroyAfterSeconds;
        }
    }

    public void Launch(Vector3 initialVelocity)
    {
        _isLaunched = true;
        _velocity = initialVelocity;

        if (_rigidbody != null)
        {
            _rigidbody.useGravity = true;
            _rigidbody.velocity = initialVelocity;
            if (applySpin)
            {
                _rigidbody.angularVelocity = spinDegreesPerSecond * Mathf.Deg2Rad;
            }
        }
    }

    private void Update()
    {
        if (!_isLaunched)
        {
            return;
        }

        if (_rigidbody == null)
        {
            _velocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
            transform.position += _velocity * Time.deltaTime;

            if (alignToVelocity && _velocity.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(_velocity.normalized, Vector3.up);
            }

            if (applySpin)
            {
                transform.Rotate(spinDegreesPerSecond * Time.deltaTime, Space.Self);
            }
        }

        if (destroyAfterSeconds > 0f && Time.time >= _destroyAt)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (!_isLaunched || _rigidbody == null)
        {
            return;
        }

        if (Mathf.Abs(gravityMultiplier - 1f) > 0.001f)
        {
            _rigidbody.AddForce(Physics.gravity * (gravityMultiplier - 1f), ForceMode.Acceleration);
        }

        if (alignToVelocity)
        {
            var velocity = _rigidbody.velocity;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                _rigidbody.MoveRotation(Quaternion.LookRotation(velocity.normalized, Vector3.up));
            }
        }
    }

    private void OnValidate()
    {
        gravityMultiplier = Mathf.Max(0f, gravityMultiplier);
        destroyAfterSeconds = Mathf.Max(0f, destroyAfterSeconds);
    }
}
