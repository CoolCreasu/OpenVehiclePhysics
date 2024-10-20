namespace OVP.VehiclePhysics
{
    using System;
    using UnityEngine;
    using UnityEngine.Experimental.GlobalIllumination;

    public class Wheel : MonoBehaviour
    {
        public float steerAngle = 0.0f;
        public float load = 0.0f;
        public float angularVelocity = 0.0f;

        public float engineTorque = 0.0f;
        public float brakeTorque = 8000.0f;

        [Header("Wheel")]
        [SerializeField] private float radius = 0.34f;
        [SerializeField] private float mass = 20.0f;

        [Header("Suspension")]
        [SerializeField] private float suspensionLength = 0.5f;
        [SerializeField] private float springRate = 50000.0f;
        [SerializeField] private float damperRate = 2500.0f;

        private Transform cachedTransform = default;
        private Transform visualTransform = default;
        private Rigidbody cachedRigidbody = default;

        private Vector3 cachedPosition = Vector3.zero;
        private Vector3 wheelForward = Vector3.zero;
        private Vector3 wheelRight = Vector3.zero;
        private Vector3 wheelUp = Vector3.zero;

        private bool isGrounded = false;
        private RaycastHit hitResult = default;

        private Vector3 projectedForward = Vector3.zero;
        private Vector3 projectedRight = Vector3.zero;

        private float currentSuspensionLength = 0.0f;
        private float previousSuspensionLength = 0.0f;

        private Vector3 globalVelocity = Vector3.zero;
        private Vector2 localVelocity = Vector2.zero;

        private Vector2 slip = Vector2.zero;

        private Vector2 localGravityCounterForce = Vector2.zero;
        private Vector2 localVelocityCounterForce = Vector2.zero;
        private Vector2 localCombinedCounterForce = Vector2.zero;

        private float inertia = 0.5f;
        private float visualRotation = 0.0f;

        private Vector3 wheelForce = Vector3.zero;

        private float dT = 0.0f;

        private void OnEnable()
        {
            cachedTransform = GetComponent<Transform>();
            visualTransform = cachedTransform.GetChild(0);
            cachedRigidbody = cachedTransform.GetComponentInParent<Rigidbody>();

            if (cachedRigidbody == null)
            {
                enabled = false;
                return;
            }

            currentSuspensionLength = suspensionLength;
            inertia = radius * radius * mass * 0.5f;
        }

        private void FixedUpdate()
        {
            dT = Time.fixedDeltaTime;

            cachedPosition = cachedTransform.position;
            Quaternion steerRotation = Quaternion.Euler(0.0f, steerAngle, 0.0f);
            Quaternion combinedRotation = cachedTransform.rotation * steerRotation;
            wheelForward = combinedRotation * Vector3.forward;
            wheelRight = combinedRotation * Vector3.right;
            wheelUp = combinedRotation * Vector3.up;

            Debug.DrawRay(cachedPosition, wheelForward, Color.blue, 0.0f, false);
            Debug.DrawRay(cachedPosition, wheelRight, Color.red, 0.0f, false);
            Debug.DrawRay(cachedPosition, wheelUp, Color.green, 0.0f, false);

            isGrounded = Physics.Raycast(cachedPosition, -wheelUp, out hitResult, suspensionLength + radius);

            projectedForward = Vector3.Cross(hitResult.normal, -wheelRight).normalized;
            projectedRight = Vector3.Cross(hitResult.normal, wheelForward).normalized;

            previousSuspensionLength = currentSuspensionLength;
            currentSuspensionLength = isGrounded ? hitResult.distance - radius : suspensionLength;
            load = ((suspensionLength - currentSuspensionLength) * springRate) + (((previousSuspensionLength - currentSuspensionLength) / dT) * damperRate);
            load = load > 0 ? load : 0;

            cachedRigidbody.AddForceAtPosition(load * wheelUp, cachedPosition);

            globalVelocity = cachedRigidbody.GetPointVelocity(hitResult.point) - (hitResult.rigidbody != null ? hitResult.rigidbody.GetPointVelocity(hitResult.point) : Vector3.zero);
            globalVelocity = Vector3.ProjectOnPlane(globalVelocity, hitResult.normal);
            localVelocity.y = Vector3.Dot(globalVelocity, projectedForward);
            localVelocity.x = Vector3.Dot(globalVelocity, projectedRight);

            slip.x = -(localVelocity.x);
            slip.y = -(localVelocity.y - angularVelocity * radius);

            // === angular velocity integration ===

            // engine torque
            angularVelocity += engineTorque / inertia * dT;

            // friction torque
            float frictionTorque = (localCombinedCounterForce.y * radius);
            float limit = Mathf.Abs(slip.y) / dT * inertia;
            angularVelocity -= Mathf.Clamp((localCombinedCounterForce.y * radius), -limit, limit) / inertia * dT;

            // brake torque
            limit = Math.Abs(angularVelocity) / dT * inertia;
            angularVelocity -= Math.Sign(angularVelocity) * Mathf.Min(brakeTorque, limit) / inertia * dT;

            // linear velocity integration
            Vector3 worldGravityCounterForce = -Physics.gravity.normalized * (load / Mathf.Max(Mathf.Abs(Vector3.Dot(-Physics.gravity.normalized, hitResult.normal)), 1e-6f));
            localGravityCounterForce.x = Vector3.Dot(worldGravityCounterForce, projectedRight);
            localGravityCounterForce.y = Vector3.Dot(worldGravityCounterForce, projectedForward);
            localVelocityCounterForce = slip * (load / Physics.gravity.magnitude) / dT;
            localCombinedCounterForce = localGravityCounterForce + localVelocityCounterForce; // TODO perform slip curve before combining and then limit again by slip curve limit
            localCombinedCounterForce = Vector3.ClampMagnitude(localCombinedCounterForce, load);

            wheelForce = (projectedForward * localCombinedCounterForce.y) + (projectedRight * localCombinedCounterForce.x);
            cachedRigidbody.AddForceAtPosition(wheelForce, cachedPosition - wheelUp * currentSuspensionLength);

            visualTransform.localPosition = new Vector3(0.0f, -currentSuspensionLength, 0.0f);
            visualRotation += angularVelocity * Mathf.Rad2Deg * dT;
            visualRotation = visualRotation % 360;
            visualTransform.localEulerAngles = new Vector3(visualRotation, steerAngle, 0.0f);
        }
    }
}