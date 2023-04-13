using OVP.Utilities;
using UnityEngine;

namespace OVP.VehicleSystems
{
    public class WheelColliderV2 : MonoBehaviour
    {
        private Rigidbody _rigidbody = default;

        [Header("Suspension")]
        [SerializeField] private float _suspensionLength = 0.3f;
        [SerializeField] private float _suspensionSpring = 35000.0f;
        [SerializeField] private float _suspensionDamper = 4500.0f;

        [Header("Wheel")]
        [SerializeField] private float _wheelRadius = 0.5f;
        [SerializeField] private float _wheelMass = 20.0f;

        private Vector3 _position = Vector3.zero;
        private Quaternion _rotation = Quaternion.identity;
        private Quaternion _localRotation = Quaternion.identity;
        private Vector3 _localForward = Vector3.zero;
        private Vector3 _localUp = Vector3.zero;
        private Vector3 _localRight = Vector3.zero;

        private float _slipX = 0.0f;
        private float _slipZ = 0.0f;
        private float _slipFeedbackForce = 0.0f;
        private float _slipAngle = 0.0f;
        private float _slipAnglePeak = 8.0f;
        private float _slipAngleDynamic = 0.0f;

        private float _wheelRotation = 0.0f;
        private Vector3 _localVelocity = Vector3.zero;
        private Vector3 _wheelPosition = Vector3.zero;
        private bool _isGrounded = false;
        private bool _isLocked = false;

        private RaycastHit _raycastHit = default;
        private float _suspensionCompression = 0.0f;
        private float _suspensionCompressionPrevious = 0.0f;

        public bool DebugMode { get; set; } = false;
        public float DriveTorque { get; set; } = 0.0f;
        public float BrakeTorque { get; set; } = 0.0f;
        public float SteerAngle { get; set; } = 0.0f;
        public float AngularVelocity { get; private set; } = 0.0f;
        public float WheelInertia { get; private set; } = 2.5f;

        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
            if (!_rigidbody)
            {
                Debug.LogWarning("WheelColliderV2 requires an attached Rigidbody to function.");
            }
        }

        private void Start()
        {
            WheelInertia = _wheelRadius * _wheelRadius * _wheelMass * 0.5f;
        }

        public void GetWorldPose(out Vector3 position, out Quaternion rotation)
        {
            position = _wheelPosition;
            rotation = _rotation * Quaternion.Euler(_wheelRotation, SteerAngle, 0.0f);

        }

        public void UpdatePhysics(float deltaTime, float deltaTimeInverted)
        {
            if (!_rigidbody) return;

            // ===== CACHING =====
            _position = transform.position;
            _rotation = transform.rotation;

            // Calculate the local Forward, Up and Right directions
            _localRotation = Quaternion.Euler(0, SteerAngle, 0);
            _localForward = transform.TransformDirection(_localRotation * Vector3.forward);
            _localUp = transform.TransformDirection(_localRotation * Vector3.up);
            _localRight = transform.TransformDirection(_localRotation * Vector3.right);

            // ===== DEBUG =====
            if (DebugMode)
            {
                Debug.DrawRay(transform.position, _localForward, Color.blue);
                Debug.DrawRay(transform.position, _localUp, Color.green);
                Debug.DrawRay(transform.position, _localRight, Color.red);
            }

            // ===== RAYCAST =====
            _isGrounded = Physics.Raycast(_position, -_localUp, out _raycastHit, _suspensionLength + _wheelRadius);
            if (!_isGrounded) _raycastHit.normal = _localUp;

            // ===== COMPRESSION =====
            _suspensionCompressionPrevious = _isGrounded ? _suspensionCompression : 0.0f;
            _suspensionCompression = _isGrounded ? (_suspensionLength + _wheelRadius) - _raycastHit.distance : 0.0f;

            // ===== POSITION =====
            _wheelPosition = _position - _localUp * (_suspensionLength - _suspensionCompression);

            // ===== LOCAL VELOCITY =====
            Vector3 worldVelocity = _rigidbody.GetPointVelocity(_wheelPosition) - (_raycastHit.rigidbody ? _raycastHit.rigidbody.GetPointVelocity(_wheelPosition) : Vector3.zero);
            _localVelocity.x = Vector3.Dot(worldVelocity, _localRight);
            _localVelocity.y = Vector3.Dot(worldVelocity, _localUp);
            _localVelocity.z = Vector3.Dot(worldVelocity, _localForward);

            // ===== SUSPENSION =====
            // compression = 1 - ((raycast distance - wheel radius) / max suspension length)
            float suspensionForce = (_suspensionCompression * _suspensionSpring) + ((_suspensionCompression - _suspensionCompressionPrevious) * deltaTimeInverted * _suspensionDamper);
            _rigidbody.AddForceAtPosition(_localUp * suspensionForce, _position);

            // ===== TIRE LOAD =====
            float load = Mathf.Max(suspensionForce, 0.0f);

            // ===== ANGULAR VELOCITY =====
            // acceleration = torque / moment of inertia => velocity = acceleration * time
            AngularVelocity = AngularVelocity + (DriveTorque - _slipFeedbackForce) / WheelInertia * deltaTime;
            // Sign needed for zero cross detection
            float signedAngularVelocity = Mathf.Sign(AngularVelocity);
            // Brake torque always in opposite direction of rotation
            AngularVelocity = AngularVelocity - (signedAngularVelocity * BrakeTorque) / WheelInertia * deltaTime;

            // Zero cross detection: If the angular velocity crosses 0, it indicates braking.
            // To prevent undesired oscillation or oscillatory behavior and achieve a complete stop,
            // we set the angular velocity to 0 and mark the wheel as locked.
            if (Mathf.Sign(AngularVelocity) != signedAngularVelocity)
            {
                AngularVelocity = 0.0f;
                _isLocked = true;
            }
            else
            {
                _isLocked = false;
            }

            // ===== WHEEL ROTATION =====
            _wheelRotation = _wheelRotation + AngularVelocity * Mathf.Rad2Deg * deltaTime;
            _wheelRotation = Mathf.Repeat(_wheelRotation, 360.0f); // Make it stay in 0-360



            // TODO SLIP X and Z calculations   //

            // ===== SLIP Z ===== //
            float _slipZMax = Mathf.Clamp(MathExtensions.SafeDivide((AngularVelocity - (_localVelocity.z / _wheelRadius)) * deltaTimeInverted * WheelInertia, load * _wheelRadius), -1.0f, 1.0f);
            float _actualSlip = _isLocked ? Mathf.Sign(_localVelocity.z) : _slipZMax;
            _slipZ = _slipZ + (_actualSlip - _slipZ) * Mathf.Clamp01(Mathf.Abs(_localVelocity.z) / 0.01f * deltaTime);
            _slipZ = Mathf.Clamp(_slipZ, -1.0f, 1.0f);

            // ===== SLIP X ===== //
            _slipAngle = Mathf.Atan2(-_localVelocity.x, Mathf.Abs(_localVelocity.z)) * Mathf.Rad2Deg;
            float delta = (_localVelocity.x - 3.0f) / (6.0f - 3.0f);
            _slipAngle = Mathf.Lerp(_slipAnglePeak * Mathf.Sign(-_localVelocity.x), _slipAngle, delta);
            _slipAngleDynamic = _slipAngleDynamic + (_slipAngle - _slipAngleDynamic) * Mathf.Clamp01(Mathf.Abs(_localVelocity.x) / 0.01f * deltaTime);
            _slipX = Mathf.Clamp(_slipAngleDynamic / _slipAnglePeak, -1.0f, 1.0f);

            // ===== COMBINED SLIP =====
            Vector2 combinedTireSlip = Vector2.ClampMagnitude(new Vector2(_slipX, _slipZ), 1.0f);

            // ===== SURFACE =====
            Vector3 forward = Vector3.ProjectOnPlane(_localForward, _raycastHit.normal).normalized;
            Vector3 right = Vector3.ProjectOnPlane(_localRight, _raycastHit.normal).normalized;

            _slipFeedbackForce = combinedTireSlip.y * load * _wheelRadius;

            // ===== TIRE FORCE =====
            if (_isGrounded) _rigidbody.AddForceAtPosition(forward * combinedTireSlip.y * load + right * combinedTireSlip.x * load, _wheelPosition);
        }

        /*
        public float NormalizedLateralPacejka(float slipAngle)
        {
            float B = 20.0f;
            float C = 2.0f;
            float D = 2.0f;
            float E = 0.3f;

            return D * Mathf.Sin(C * Mathf.Atan(B * slipAngle - E * (B * slipAngle - Mathf.Atan(B * slipAngle))));
        }
        */
    }
}