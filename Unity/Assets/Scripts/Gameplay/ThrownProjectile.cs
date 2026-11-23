using UnityEngine;

[DisallowMultipleComponent]
public class ThrownProjectile : MonoBehaviour
{
    [Header("Motion")]
    [SerializeField] private float gravityMultiplier = 1f;
    [SerializeField] private bool alignToVelocity = true;

    [Header("Initial Rotation")]
    [SerializeField] private bool randomizeInitialRotation = true;
    [SerializeField] private Vector3 initialRotationRandomDegrees = new Vector3(10f, 10f, 20f);

    [Header("Spin / Torque")]
    [SerializeField] private bool applySpin = true;
    [SerializeField] private Vector3 spinDegreesPerSecond = new Vector3(0f, 0f, 420f);
    [SerializeField] private Vector3 spinRandomDegreesPerSecond = new Vector3(60f, 60f, 120f);
    [SerializeField] private bool addRandomTorqueImpulse;
    [SerializeField] private float randomTorqueImpulse = 0.2f;

    [Header("Lifetime")]
    [SerializeField] private float destroyAfterSeconds = 6f;

    private Rigidbody _rigidbody;
    private Vector3 _velocity;
    private Vector3 _currentSpinDegreesPerSecond;
    private bool _isLaunched;
    private float _destroyAt;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 initialVelocity)
    {
        _isLaunched = true;
        _velocity = initialVelocity;
        _currentSpinDegreesPerSecond = BuildSpinDegreesPerSecond();

        if (randomizeInitialRotation)
        {
            transform.rotation *= Quaternion.Euler(
                Random.Range(-initialRotationRandomDegrees.x, initialRotationRandomDegrees.x),
                Random.Range(-initialRotationRandomDegrees.y, initialRotationRandomDegrees.y),
                Random.Range(-initialRotationRandomDegrees.z, initialRotationRandomDegrees.z));
        }

        if (destroyAfterSeconds > 0f)
        {
            _destroyAt = Time.time + destroyAfterSeconds;
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            _rigidbody.linearVelocity = initialVelocity;
            if (applySpin)
            {
                _rigidbody.angularVelocity = _currentSpinDegreesPerSecond * Mathf.Deg2Rad;
            }
            if (addRandomTorqueImpulse && randomTorqueImpulse > 0f)
            {
                _rigidbody.AddTorque(Random.onUnitSphere * randomTorqueImpulse, ForceMode.Impulse);
            }
        }
    }

    public void SetHeldState(bool isHeld)
    {
        if (!isHeld)
        {
            return;
        }

        _isLaunched = false;
        _velocity = Vector3.zero;
        _currentSpinDegreesPerSecond = Vector3.zero;
        _destroyAt = 0f;

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
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
                transform.Rotate(_currentSpinDegreesPerSecond * Time.deltaTime, Space.Self);
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
            var velocity = _rigidbody.linearVelocity;
            if (velocity.sqrMagnitude > 0.0001f)
            {
                _rigidbody.MoveRotation(Quaternion.LookRotation(velocity.normalized, Vector3.up));
            }
        }
    }

    private void OnValidate()
    {
        gravityMultiplier = Mathf.Max(0f, gravityMultiplier);
        randomTorqueImpulse = Mathf.Max(0f, randomTorqueImpulse);
        destroyAfterSeconds = Mathf.Max(0f, destroyAfterSeconds);
    }

    private Vector3 BuildSpinDegreesPerSecond()
    {
        if (!applySpin)
        {
            return Vector3.zero;
        }

        return new Vector3(
            spinDegreesPerSecond.x + Random.Range(-spinRandomDegreesPerSecond.x, spinRandomDegreesPerSecond.x),
            spinDegreesPerSecond.y + Random.Range(-spinRandomDegreesPerSecond.y, spinRandomDegreesPerSecond.y),
            spinDegreesPerSecond.z + Random.Range(-spinRandomDegreesPerSecond.z, spinRandomDegreesPerSecond.z));
    }
}
