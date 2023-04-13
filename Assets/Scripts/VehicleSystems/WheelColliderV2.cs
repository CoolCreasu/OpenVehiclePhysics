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

        private float _frictionTorque = 0.0f;

        private float _wheelRotation = 0.0f;

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

            Vector3 transformPosition = transform.position;

            Quaternion localRotation = Quaternion.Euler(0.0f, SteerAngle, 0.0f);
            Vector3 wheelRight = transform.TransformDirection(localRotation * Vector3.right);
            Vector3 wheelUp = transform.TransformDirection(localRotation * Vector3.up);
            Vector3 wheelForward = transform.TransformDirection(localRotation * Vector3.forward);

            bool isGrounded = Physics.Raycast(transformPosition, -wheelUp, out RaycastHit raycastHit, _suspensionLength + _wheelRadius);
            if (!isGrounded) raycastHit.normal = wheelUp;

            float _suspensionCompressionPrevious = isGrounded ? _suspensionCompression : 0.0f;
            _suspensionCompression = isGrounded ? (_suspensionLength + _wheelRadius) - raycastHit.distance : 0.0f;

            Vector3 wheelPosition = transformPosition - wheelUp * (_suspensionLength - _suspensionCompression);

            Vector3 worldVelocity = _rigidbody.GetPointVelocity(wheelPosition) - (raycastHit.rigidbody ? raycastHit.rigidbody.GetPointVelocity(wheelPosition) : Vector3.zero);
            Vector3 localVelocity = Vector3.zero;
            localVelocity.x = Vector3.Dot(worldVelocity, wheelRight);
            localVelocity.y = Vector3.Dot(worldVelocity, wheelUp);
            localVelocity.z = Vector3.Dot(worldVelocity, wheelForward);

            float suspensionForce = (_suspensionCompression * _suspensionSpring) + (_suspensionCompression - _suspensionCompressionPrevious) / deltaTime * _suspensionDamper;
            _rigidbody.AddForceAtPosition(wheelUp * suspensionForce, wheelPosition);

            float load = Mathf.Max(suspensionForce, 0.0f);

            ///// SLIP -----------------

            AngularVelocity = AngularVelocity + ((DriveTorque - _frictionTorque) / WheelInertia * deltaTime);

            float slipRatio = MathExtensions.SafeDivide(Mathf.Abs(AngularVelocity * _wheelRadius) - localVelocity.z, Mathf.Abs(localVelocity.z));
            //float slipRatio = MathExtensions.SafeDivide(AngularVelocity * _wheelRadius, localVelocity.z) - 1.0f;
            float longitudinalPacejka = Pacejka(slipRatio, 10.0f, 1.9f, 1.0f, 0.97f);

            Vector3 forward = Vector3.ProjectOnPlane(wheelForward, raycastHit.normal);
            Vector3 force = longitudinalPacejka * forward * load;

            _frictionTorque = longitudinalPacejka * load * _wheelRadius;

            force = Vector3.ClampMagnitude(force, load);

            _rigidbody.AddForceAtPosition(force, wheelPosition);

            //Debug.Log(slipRatio);
        }

        private float Pacejka(float slip, float B, float C, float D, float E)
        {
            return D * Mathf.Sin(C * Mathf.Atan(B * slip - E * (B * slip - Mathf.Atan(B * slip))));
        }

        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (this.isActiveAndEnabled)
            {
                Handles.color = Color.green;
                GetWorldPose(out Vector3 position, out Quaternion rotation);
                Handles.DrawWireDisc(position, rotation * Vector3.right, _wheelRadius);
            }
#endif
        }

        public void GetWorldPose(out Vector3 position, out Quaternion rotation)
        {
            position = transform.position - transform.up * (_suspensionLength - _suspensionCompression);
            rotation = transform.rotation * Quaternion.Euler(_wheelRotation, SteerAngle, 0.0f);
        }
    }
}