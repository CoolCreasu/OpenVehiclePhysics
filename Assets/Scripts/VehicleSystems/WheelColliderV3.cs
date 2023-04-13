using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace OVP.VehicleSystems
{
    public class WheelColliderV3 : MonoBehaviour
    {
        [Header("Suspension")]
        [SerializeField] private float _suspensionLength = 0.15f;
        [SerializeField] private float _suspensionSpring = 35000.0f;
        [SerializeField] private float _suspensionDamper = 4500.0f;
        [Header("Wheel")]
        [SerializeField] private float _wheelMass = 20.0f;
        [SerializeField] private float _wheelRadius = 0.3f;

        private float _wheelRotation = 0.0f;

        private float _frictionForce = 0.0f;

        private float _suspensionCompression = 0.0f;

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
                Debug.LogWarning("WheelColliderV3 requires an attached Rigidbody to function.");
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

            Vector3 transformPosition = transform.position;

            Quaternion localRotation = Quaternion.Euler(0.0f, SteerAngle, 0.0f);
            Vector3 wheelRight = transform.TransformDirection(localRotation * Vector3.right);
            Vector3 wheelUp = transform.TransformDirection(localRotation * Vector3.up);
            Vector3 wheelForward = transform.TransformDirection(localRotation * Vector3.forward);

            bool isGrounded = Physics.Raycast(transformPosition, -wheelUp, out RaycastHit raycastHit, _suspensionLength + _wheelRadius);
            if (!isGrounded) raycastHit.normal = wheelUp;

            float suspensionCompressionPrevious = isGrounded ? _suspensionCompression : 0.0f;
            _suspensionCompression = isGrounded ? (_suspensionLength + _wheelRadius) - raycastHit.distance : 0.0f;

            Vector3 wheelPosition = transformPosition - wheelUp * (_suspensionLength - _suspensionCompression);

            Vector3 worldVelocity = _rigidbody.GetPointVelocity(wheelPosition) - (raycastHit.rigidbody ? raycastHit.rigidbody.GetPointVelocity(wheelPosition) : Vector3.zero);
            Vector3 localVelocity;
            localVelocity.x = Vector3.Dot(worldVelocity, wheelRight);
            localVelocity.y = Vector3.Dot(worldVelocity, wheelUp);
            localVelocity.z = Vector3.Dot(worldVelocity, wheelForward);

            float suspensionForce = (_suspensionCompression * _suspensionSpring) + ((_suspensionCompression - suspensionCompressionPrevious) / deltaTime * _suspensionDamper);
            _rigidbody.AddForceAtPosition(wheelUp * suspensionForce, wheelPosition);

            float load = Mathf.Max(suspensionForce, 0.0f);
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
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