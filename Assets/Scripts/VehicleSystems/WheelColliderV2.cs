using OVP.Utilities;
using UnityEngine;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEditor;
#endif

namespace OVP.VehicleSystems
{
    public class WheelColliderV2 : MonoBehaviour
    {
        [Header("Suspension")]
        [SerializeField] private float _suspensionLength = 0.15f;
        [SerializeField] private float _suspensionSpring = 35000.0f;
        [SerializeField] private float _suspensionDamper = 4500.0f;
        [Header("Wheel")]
        [SerializeField] private float _wheelMass = 20.0f;
        [SerializeField] private float _wheelRadius = 0.3f;
        [Header("Friction")]
        [SerializeField] private float _longSlipModifier = 1.5f;

        private Vector3 _transformPosition = Vector3.zero;

        private Quaternion _localRotation = Quaternion.identity;
        private Vector3 _wheelRight = Vector3.zero;
        private Vector3 _wheelUp = Vector3.zero;
        private Vector3 _wheelForward = Vector3.zero;

        private float _previousFrictionTorque = 0.0f;

        private float _wheelRotation = 0.0f;
        private Vector3 _worldVelocity = Vector3.zero;
        private Vector3 _localVelocity = Vector3.zero;
        private Vector3 _wheelPosition = Vector3.zero;
        private bool _isGrounded = false;

        private RaycastHit _raycastHit = default;
        private float _suspensionCompression = 0.0f;
        private float _suspensionCompressionPrevious = 0.0f;

        private Rigidbody _rigidbody = default;

        public float DriveTorque { get; set; } = 0.0f;
        public float BrakeTorque { get; set; } = 0.0f;
        public float SteerAngle { get; set; } = 0.0f;
        public float AngularVelocity { get; set; } = 0.0f;
        public float WheelInertia { get; set; } = 0.0f;

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

        private void FixedUpdate()
        {
            if (!_rigidbody) return;

            float deltaTime = Time.fixedDeltaTime;

            _transformPosition = transform.position;

            _localRotation = Quaternion.Euler(0.0f, SteerAngle, 0.0f);
            _wheelRight = transform.TransformDirection(_localRotation * Vector3.right);
            _wheelUp = transform.TransformDirection(_localRotation * Vector3.up);
            _wheelForward = transform.TransformDirection(_localRotation * Vector3.forward);

            _isGrounded = Physics.Raycast(_transformPosition, -_wheelUp, out _raycastHit, _suspensionLength + _wheelRadius);
            if (!_isGrounded) _raycastHit.normal = _wheelUp;

            _suspensionCompressionPrevious = _isGrounded ? _suspensionCompression : 0.0f;
            _suspensionCompression = _isGrounded ? (_suspensionLength + _wheelRadius) - _raycastHit.distance : 0.0f;

            _wheelPosition = _transformPosition - _wheelUp * (_suspensionLength - _suspensionCompression);

            _worldVelocity = _rigidbody.GetPointVelocity(_wheelPosition) - (_raycastHit.rigidbody ? _raycastHit.rigidbody.GetPointVelocity(_wheelPosition) : Vector3.zero);
            _localVelocity.x = Vector3.Dot(_worldVelocity, _wheelRight);
            _localVelocity.y = Vector3.Dot(_worldVelocity, _wheelUp);
            _localVelocity.z = Vector3.Dot(_worldVelocity, _wheelForward);

            float suspensionForce = (_suspensionCompression * _suspensionSpring) + (_suspensionCompression - _suspensionCompressionPrevious) / deltaTime * _suspensionDamper;
            _rigidbody.AddForceAtPosition(_wheelUp * suspensionForce, _wheelPosition);

            float load = Mathf.Max(suspensionForce, 0.0f);

            AngularVelocity = AngularVelocity + (DriveTorque-_previousFrictionTorque) / WheelInertia * deltaTime;

            _wheelRotation = _wheelRotation + (AngularVelocity * Mathf.Rad2Deg * deltaTime);
            _wheelRotation = Mathf.Repeat(_wheelRotation, 360.0f); // keep within 360 degrees

            Vector3 forward = Vector3.ProjectOnPlane(_wheelForward, _raycastHit.normal).normalized;
            Vector3 right = Vector3.ProjectOnPlane(_wheelRight, _raycastHit.normal).normalized;

            Vector3 totalForce = Vector3.ClampMagnitude(forward * -_localVelocity.z * load * _longSlipModifier + right * -_localVelocity.x * load, load);

            _rigidbody.AddForceAtPosition(totalForce, _wheelPosition);
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Handles.color = Color.green;
            GetWorldPose(out Vector3 position, out Quaternion rotation);
            Handles.DrawWireDisc(position, rotation * Vector3.right, _wheelRadius);
#endif
        }

        public void GetWorldPose(out Vector3 position, out Quaternion rotation)
        {
            position = transform.position - transform.up * (_suspensionLength - _suspensionCompression);
            rotation = transform.rotation * Quaternion.Euler(_wheelRotation, SteerAngle, 0.0f);
        }
    }
}

#region OLD
/*
namespace OVP.VehicleSystems
{
    public class WheelColliderV2 : MonoBehaviour
    {
        private Rigidbody _rigidbody = default;

        [Header("Suspension")]
        [SerializeField] private float _suspensionLength = 0.15f;
        [SerializeField] private float _suspensionSpring = 35000.0f;
        [SerializeField] private float _suspensionDamper = 4500.0f;
        [Header("Wheel")]
        [SerializeField] private float _wheelMass = 20.0f;
        [SerializeField] private float _wheelRadius = 0.3f;

        private Vector3 _transformPosition = Vector3.zero;
        private Quaternion _transformRotation = Quaternion.identity;

        private Quaternion _localRotation = Quaternion.identity;
        private Vector3 _wheelRight = Vector3.zero;
        private Vector3 _wheelUp = Vector3.zero;
        private Vector3 _wheelForward = Vector3.zero;

        private float _lastFrictionForce = 0.0f;
        private float _lastFrictionTorque = 0.0f;

        private float _wheelRotation = 0.0f;
        private Vector3 _worldVelocity = Vector3.zero;
        private Vector3 _localVelocity = Vector3.zero;
        private Vector3 _wheelPosition = Vector3.zero;
        private bool _isGrounded = false;
        private bool _isLocked = false;

        private RaycastHit _raycastHit = default;
        private float _suspensionCompression = 0.0f;
        private float _suspensionCompressionPrevious = 0.0f;

        public float DriveTorque { get; set; } = 0.0f;
        public float BrakeTorque { get; set; } = 0.0f;
        public float SteerAngle { get; set; } = 0.0f;
        public float AngularVelocity { get; private set; } = 0.0f;
        public float WheelInertia { get; private set; } = 0.9f;

        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!_rigidbody)
            {
                Debug.LogWarning("WheelColliderV2 requires an attached Rigidbody to function.");
            }
#endif
        }

        private void Start()
        {
            WheelInertia = _wheelRadius * _wheelRadius * _wheelMass * 0.5f;
        }

        public void GetWorldPose(out Vector3 position, out Quaternion rotation)
        {
            position = transform.position - transform.up * (_suspensionLength - _suspensionCompression);
            rotation = transform.rotation * Quaternion.Euler(0.0f, SteerAngle, 0.0f);
        }

        public void UpdatePhysics(float deltaTime)
        {
            // ===== Caching ===== //
            _transformPosition = transform.position;
            _transformRotation = transform.rotation;

            // ===== Calculate Right, Up and Forward directions ===== //
            _localRotation = Quaternion.Euler(0.0f, SteerAngle, 0.0f);
            _wheelRight = transform.TransformDirection(_localRotation * Vector3.right);
            _wheelUp = transform.TransformDirection(_localRotation * Vector3.up);
            _wheelForward = transform.TransformDirection(_localRotation * Vector3.forward);

            // ===== Raycast to the ground, for suspension length and all that ===== //
            _isGrounded = Physics.Raycast(_transformPosition, -_wheelUp, out _raycastHit, _suspensionLength + _wheelRadius);
            if (!_isGrounded) _raycastHit.normal = _wheelUp;

            // ===== Calculate suspension compression ===== //
            _suspensionCompressionPrevious = _isGrounded ? _suspensionCompression : 0.0f;
            _suspensionCompression = _isGrounded ? (_suspensionLength + _wheelRadius) - _raycastHit.distance : 0.0f;

            // ===== Calculate the position of the center of the wheel ===== //
            _wheelPosition = _transformPosition - _wheelUp * (_suspensionLength - _suspensionCompression);

            // ===== Get world/local velocity of the car at the wheel position ===== //
            _worldVelocity = _rigidbody.GetPointVelocity(_wheelPosition) - (_raycastHit.rigidbody ? _raycastHit.rigidbody.GetPointVelocity(_wheelPosition) : Vector3.zero);
            _localVelocity.x = Vector3.Dot(_worldVelocity, _wheelRight);
            _localVelocity.y = Vector3.Dot(_worldVelocity, _wheelUp);
            _localVelocity.z = Vector3.Dot(_worldVelocity, _wheelForward);

            // ===== Calculation of suspension forces and apply ===== //
            float suspensionForce = (_suspensionCompression * _suspensionSpring) + (_suspensionCompression - _suspensionCompressionPrevious) / deltaTime * _suspensionDamper;
            _rigidbody.AddForceAtPosition(_wheelUp * suspensionForce, _transformPosition);

            float slipRatio = MathExtensions.SafeDivide((AngularVelocity * _wheelRadius) - _localVelocity.z, Mathf.Abs(_localVelocity.z));
            Debug.DrawRay(_rigidbody.position + Vector3.up * 2, _wheelForward * slipRatio);
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            Handles.color = Color.green;
            GetWorldPose(out Vector3 position, out Quaternion rotation);
            Handles.DrawWireDisc(position, rotation * Vector3.right, _wheelRadius);
#endif
        }

        public float Pacejka(float slip, float B = 10.0f, float C = 1.9f, float D = 1.0f, float E = 0.97f)
        {
            return D * Mathf.Sin(C * Mathf.Atan(B * slip - E * (B * slip - Mathf.Atan(B * slip))));
        }
    }
}
*/
#endregion