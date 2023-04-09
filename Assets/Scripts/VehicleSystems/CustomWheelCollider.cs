using OVP.Utilities;
using UnityEngine;

namespace OVP.VehicleSystems
{
    /// <summary>
    /// Custom wheel collider that calculates and updates the world pose and physics of a wheel.
    /// </summary>
    public class CustomWheelCollider : MonoBehaviour
    {
        private Rigidbody _rigidbody = default; // Reference to the Rigidbody attached to the same GameObject

        [Header("Suspension")]
        [SerializeField] private float _suspensionLength = 0.3f; // Length of the suspension
        [SerializeField] private float _suspensionSpring = 35000.0f; // Spring force of the suspension
        [SerializeField] private float _suspensionDamper = 4500.0f; // Damping force of the suspension

        [Header("Wheel")]
        [SerializeField] private float _wheelMass = 20.0f; // Mass of the wheel
        [SerializeField] private float _wheelRadius = 0.5f; // Radius of the wheel

        private float _wheelInertia = 0.0f; // Inertia of the wheel
        private float _wheelRotation = 0.0f; // Current rotation of the wheel
        private Vector3 _localVelocity = Vector3.zero; // The local velocity of the wheel
        private Vector3 _wheelPosition = Vector3.zero; // Current position of the wheel
        private bool _isGrounded = false; // Whether the wheel is grounded or not
        private bool _isLocked = false; // Whether the wheel is locked or not

        private float _slipZ = 0.0f; // Slip value in the Z-axis (direction of travel)
        private float _slipX = 0.0f; // Slip value in the X-axis (direction perpendicular to travel)
        private float _forceZ = 0.0f; // Force applied in the Z-axis (direction of travel)
        private float _slipAngle = 0.0f; // Slip angle of the wheel
        private float _slipAnglePeak = 8.0f; // Peak slip angle of the wheel
        private float _slipAngleDynamic = 0.0f; // Dynamic slip angle of the wheel
        private float _longitudinalSlipVelocity = 0.0f; // Longitudinal slip velocity of the wheel

        private RaycastHit _raycastHit = default; // RaycastHit used for detecting ground
        private float _suspensionCompression = 0.0f; // Current compression of the suspension
        private float _suspensionCompressionPrevious = 0.0f; // Previous compression of the suspension
        private float _suspensionForceSpring = 0.0f; // Spring force applied by the suspension
        private float _suspensionForceDamper = 0.0f; // Damping force applied by the suspension

        private float _deltaTime = 0.0f; // Delta time for physics update
        private float _deltaTimeInverted = 0.0f; // Inverted delta time for physics update

        public float DriveTorque { get; set; } = 0.0f; // Drive torque applied to the wheel
        public float BrakeTorque { get; set; } = 0.0f; // Brake torque applied to the wheel
        public float WheelAngularVelocity { get; private set; } = 0.0f; // Angular velocity of the wheel

        private void Awake()
        {
            _rigidbody = GetComponentInParent<Rigidbody>(); // Get reference to the Rigidbody
            if (!_rigidbody) // If Rigidbody is not attached
            {
                Debug.LogWarning($"CustomWheelCollider requires an attached rigidbody to function.");
            }
        }

        private void Start()
        {
            _wheelInertia = _wheelRadius * _wheelRadius * _wheelMass * 0.5f;
        }

        /// <summary>
        /// Gets the current world pose of the wheel.
        /// </summary>
        /// <param name="position">The position of the wheel.</param>
        /// <param name="rotation">The rotation of the wheel.</param>
        public void GetWorldPose(out Vector3 position, out Quaternion rotation)
        {
            position = _wheelPosition; // Set the position of the wheel
            rotation = Quaternion.Euler(_wheelRotation, transform.eulerAngles.y, 0.0f); // Set the rotation of the wheel
        }

        /// <summary>
        /// Updates the physics of the wheel with the given delta time.
        /// </summary>
        /// <param name="deltaTime">The delta time for physics update.</param>
        public void UpdatePhysics(float deltaTime)
        {
            if (!_rigidbody) return; // If Rigidbody is not attached, return

            _deltaTime = deltaTime; // Set the delta time for physics update
            _deltaTimeInverted = 1.0f / deltaTime; // Calculate inverted delta time for physics update

            CheckIfGrounded();

            CalculateSuspensionCompression();
            CalculateSuspensionForces();
            ApplySuspensionForces();

            CalculateWheelPosition();
            CalculateLocalVelocity();
            CalculateWheelAcceleration();

            CalculateSlipZ();
            CalculateSlipX();
            CalculateCombinedTireForce();
        }

        /// <summary>
        /// Checks if the wheel is grounded by performing a raycast from the wheel's position
        /// towards the opposite of the wheel's up direction.
        /// </summary>
        private void CheckIfGrounded()
        {
            _isGrounded = Physics.Raycast(transform.position, -transform.up, out _raycastHit, _suspensionLength + _wheelRadius);

            // If the wheel is not grounded, set the raycast hit normal to be equal to the wheel's up direction.
            if (!_isGrounded)
            {
                _raycastHit.normal = transform.up;
            }
        }

        /// <summary>
        /// Calculates the suspension compression based on whether the wheel is grounded or not.
        /// </summary>
        private void CalculateSuspensionCompression()
        {
            // If the wheel is grounded, calculate the suspension compression based on the distance between the wheel and the raycast hit point.
            // Otherwise, set the suspension compression to 0.
            _suspensionCompressionPrevious = _isGrounded ? _suspensionCompression : 0.0f;
            _suspensionCompression = _isGrounded ? (_suspensionLength + _wheelRadius) - _raycastHit.distance : 0.0f;
        }

        /// <summary>
        /// Calculates the suspension forces including spring force and damper force.
        /// </summary>
        private void CalculateSuspensionForces()
        {
            // Calculate the spring force based on the current suspension compression and suspension spring constant.
            _suspensionForceSpring = _suspensionCompression * _suspensionSpring;
            // Calculate the damper force based on the change in suspension compression over time, inverted delta time, and suspension damper constant.
            _suspensionForceDamper = (_suspensionCompression - _suspensionCompressionPrevious) * _deltaTimeInverted * _suspensionDamper;
        }

        /// <summary>
        /// Applies the calculated suspension forces to the rigidbody of the wheel.
        /// </summary>
        private void ApplySuspensionForces()
        {
            // Apply the suspension forces to the rigidbody at the position of the wheel.
            _rigidbody.AddForceAtPosition(transform.up * (_suspensionForceSpring + _suspensionForceDamper), transform.position);
        }

        /// <summary>
        /// Calculates the position of the wheel based on the suspension length and suspension compression.
        /// </summary>
        private void CalculateWheelPosition()
        {
            // Calculate the wheel position by subtracting the product of suspension length and suspension compression from the transform position in the up direction.
            // This accounts for the compression of the suspension, resulting in the wheel being positioned above the ground when the suspension is compressed.
            _wheelPosition = transform.position - transform.up * (_suspensionLength - _suspensionCompression);
        }

        /// <summary>
        /// Calculates the local velocity of the wheel based on its position and the rigidbody velocities.
        /// </summary>
        private void CalculateLocalVelocity()
        {
            // Get the velocity of the wheel at its position in world space, and subtract the velocity of the rigidbody at that position.
            // If the raycast hit has a rigidbody, subtract the velocity of the rigidbody at the wheel position, otherwise subtract zero vector.
            // Finally, transform the resulting velocity to local space using InverseTransformDirection.
            _localVelocity = transform.InverseTransformDirection(_rigidbody.GetPointVelocity(_wheelPosition) - (_raycastHit.rigidbody ? _raycastHit.rigidbody.GetPointVelocity(_wheelPosition) : Vector3.zero));
        }

        /// <summary>
        /// Calculates the wheel acceleration based on various factors such as drive torque, brake torque, suspension compression, wheel radius, inertia, and delta time.
        /// </summary>
        private void CalculateWheelAcceleration()
        {
            // Calculate wheel angular velocity based on drive torque, force on the wheel due to suspension compression, wheel radius, inertia, and delta time
            WheelAngularVelocity = WheelAngularVelocity + ((DriveTorque - _forceZ * _wheelRadius) / _wheelInertia * _deltaTime);

            // Calculate brake torque effect on wheel angular velocity
            float sign = Mathf.Sign(WheelAngularVelocity);
            WheelAngularVelocity = WheelAngularVelocity - (sign * BrakeTorque) / _wheelInertia * _deltaTime;

            // Zero cross detection braking
            if (Mathf.Sign(WheelAngularVelocity) != sign) 
            {
                WheelAngularVelocity = 0.0f;
                _isLocked = true;
            }
            else
            {
                _isLocked = false;
            }

            // Update wheel rotation and longitudinal slip velocity
            _wheelRotation += WheelAngularVelocity * Mathf.Rad2Deg * _deltaTime;
            _longitudinalSlipVelocity = (WheelAngularVelocity * _wheelRadius) - _localVelocity.z;
        }

        /// <summary>
        /// Calculates the longitudinal slip of the wheel along the Z-axis based on various factors such as target angular velocity, target angular acceleration, maximum friction torque, slip limits, and delta time.
        /// </summary>
        private void CalculateSlipZ()
        {
            // Calculate target angular velocity based on local velocity and wheel radius
            float targetAngularVelocity = _localVelocity.z / _wheelRadius;
            // Calculate target angular acceleration based on the difference between current and target angular velocities, and inverse of delta time
            float targetAngularAcceleration = (WheelAngularVelocity - targetAngularVelocity) * _deltaTimeInverted;
            // Calculate target torque based on target angular acceleration and wheel inertia
            float targetTorque = targetAngularAcceleration * _wheelInertia;
            // Calculate maximum friction torque based on suspension forces, wheel radius, and friction constant of the surface
            float maxFrictionTorque = (_suspensionForceSpring + _suspensionForceDamper) * _wheelRadius * 1.0f; // 1.0f is the friction constant of the surface Mu
            // Calculate slipZMax, which is the maximum longitudinal slip, clamped between -100.0f and 100.0f
            float slipZMax = Mathf.Clamp(MathExtensions.SafeDivide(targetTorque, maxFrictionTorque), -100.0f, 100.0f);
            // Calculate actual slip based on whether the wheel is locked or not
            float actualSlip = _isLocked ? Mathf.Sign(_longitudinalSlipVelocity) : slipZMax;
            // Update slipZ based on actual slip and longitudinal slip velocity, clamped with a time-based interpolation factor
            _slipZ += (actualSlip - _slipZ) * Mathf.Clamp01(Mathf.Abs(_longitudinalSlipVelocity) / 0.005f * _deltaTime);
        }

        /// <summary>
        /// Calculates the lateral slip of the wheel along the X-axis based on various factors such as local velocity, slip angles, slip angle peak, slip angle dynamic, and delta time.
        /// </summary>
        private void CalculateSlipX()
        {
            // Calculate slip angle based on local velocity along the X-axis and absolute value of local velocity along the Z-axis
            _slipAngle = Mathf.Atan2(-_localVelocity.x, Mathf.Abs(_localVelocity.z)) * Mathf.Rad2Deg;
            // Calculate delta, which is a normalized value of local velocity magnitude relative to a range of 3.0f to 6.0f
            float delta = (_localVelocity.magnitude - 3.0f) / (6.0f - 3.0f);
            // Calculate slip angle based on slip angle peak, sign of local velocity along the X-axis, and delta
            float slipAngle = Mathf.Lerp(_slipAnglePeak * Mathf.Sign(-_localVelocity.x), _slipAngle, delta);
            // Update slip angle dynamic based on slip angle, local velocity along the X-axis, delta time, and a time-based interpolation factor
            _slipAngleDynamic = Mathf.Clamp(_slipAngleDynamic + ((slipAngle - _slipAngleDynamic) * Mathf.Clamp01(Mathf.Abs(_localVelocity.x) / 0.01f * _deltaTime)), -90.0f, 90.0f);
            // Calculate slipX, which is the lateral slip, based on slip angle dynamic and slip angle peak, clamped between -1.0f and 1.0f
            _slipX = Mathf.Clamp(MathExtensions.SafeDivide(_slipAngleDynamic, _slipAnglePeak), -1.0f, 1.0f);
        }

        /// <summary>
        /// Calculates the combined tire force to be applied to the wheel, taking into account the suspension force, slip angles, and ground contact normal.
        /// </summary>
        private void CalculateCombinedTireForce()
        {
            // Calculate the effective load on the tire, which is the maximum value between suspension force spring and suspension force damper
            float load = Mathf.Max(_suspensionForceSpring + _suspensionForceDamper, 0.0f);
            // Calculate the combined tire slip as a Vector2, clamped to a magnitude of 1.0f, based on slipX and slipZ
            Vector2 combinedTireSlip = Vector2.ClampMagnitude(new Vector2(_slipX, _slipZ), 1.0f);
            // Calculate the forward and right directions of the wheel, projected onto the plane defined by the ground contact normal, and normalized
            Vector3 forward = Vector3.ProjectOnPlane(transform.forward, _raycastHit.normal).normalized;
            Vector3 right = Vector3.ProjectOnPlane(transform.right, _raycastHit.normal).normalized;
            // Calculate the force along the Z-axis (forward direction) based on combined tire slip and the effective load on the tire
            _forceZ = combinedTireSlip.y * load;
            // Calculate the force along the forward and right directions based on combined tire slip, effective load on the tire, and corresponding directions
            Vector3 forceForward = forward * combinedTireSlip.y * load;
            Vector3 forceRight = right * combinedTireSlip.x * load;
            // Apply the combined tire force to the wheel's position if the wheel is grounded
            if (_isGrounded)
            {
                _rigidbody.AddForceAtPosition(forceForward + forceRight, _wheelPosition);
            }
        }
    }
}