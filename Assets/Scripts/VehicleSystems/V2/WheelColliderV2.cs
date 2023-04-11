using UnityEngine;

namespace OVP.VehicleSystems
{
    public class WheelColliderV2 : MonoBehaviour
    {
        private Rigidbody _rigidbody = default;

        [Header("Suspension")]
        [SerializeField] private float _suspensionLength = 0.3f;
        [SerializeField] private float _suspensionSpring = 35000.0f;
        [SerializeField] private float _suspensionDamping = 4500.0f;

        [Header("Wheel")]
        [SerializeField] private float _wheelRadius = 0.5f;

        private Vector3 _position = Vector3.zero;
        private Quaternion _localRotation = Quaternion.identity;
        private Vector3 _localForward = Vector3.zero;
        private Vector3 _localUp = Vector3.zero;
        private Vector3 _localRight = Vector3.zero;

        private Vector3 _wheelPosition = Vector3.zero;
        private bool _isGrounded = false;

        private RaycastHit _raycastHit = default;
        private float _suspensionCompression = 0.0f;
        private float _suspensionCompressionPrevious = 0.0f;

        public bool DebugMode { get; set; }
        public float SteerAngle { get; set; }

        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody>();
            if (!_rigidbody)
            {
                Debug.LogWarning("WheelColliderV2 requires an attached Rigidbody to function.");
            }
        }

        public void GetWorldPose(out Vector3 position)
        {
            position = _wheelPosition;
        }

        public void UpdatePhysics(float deltaTime, float deltaTimeInverted)
        {
            // ===== CACHING =====
            _position = transform.position;

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

            // ===== SUSPENSION =====

            // Below is a way to get the compression of the suspension
            // compression = 1 - ((raycast distance - wheel radius) / max suspension length)
        }
    }
}