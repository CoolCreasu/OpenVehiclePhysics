/*
 * OpenVehiclePhysics
 * 
 * Author: CoolCreasu
 * Created: 20/10/2024
 * 
 * Description:
 * This script simulates vehicle wheel behavior. 
 * 
 */

namespace OVP.VehiclePhysics
{
    using UnityEngine;

    public class Wheel : MonoBehaviour
    {
        public float motorTorque = 0.0f;
        public float brakeTorque = 0.0f;

        public float load = 0.0f;
        public float steerAngle = 0.0f;
        public float angularVelocity = 0.0f;

        [Header("Wheel")]
        [SerializeField] private float wheelRadius = 0.34f;
        [SerializeField] private float inertia = 1.5f;

        [Header("Suspension")]
        [SerializeField] private float suspensionLength = 0.5f;
        [SerializeField] private float springRate = 50000.0f;
        [SerializeField] private float damperRate = 2500.0f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionLayers = -1;

        private Transform cachedTransform = default;
        private Transform visualTransform = default;
        private Rigidbody cachedRigidbody = default;

        private Vector3 cachedPosition = Vector3.zero;
        private Vector3 wheelForward = Vector3.zero;
        private Vector3 wheelRight = Vector3.zero;
        private Vector3 wheelUp = Vector3.zero;

        private bool isGrounded = false;
        private RaycastHit hitResult = default;

        private float previousSuspensionLength = 0.0f;
        private float currentSuspensionLength = 0.0f;

        private Vector3 projectedForward = Vector3.zero;
        private Vector3 projectedRight = Vector3.zero;

        private Vector3 worldVelocity = Vector3.zero;
        private Vector2 localVelocity = Vector2.zero;

        private Vector2 slip = Vector2.zero;

        private Vector2 frictionTireForce = Vector2.zero;

        private Vector2 localTireForce = Vector2.zero;
        private Vector3 worldTireForce = Vector3.zero;

        private float visualRotation = 0.0f;

        private float deltaTime = 0.02f;

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
        }
        private void FixedUpdate()
        {
            deltaTime = Time.fixedDeltaTime;

            cachedPosition = cachedTransform.position;
            Quaternion steerRotation = Quaternion.Euler(0.0f, steerAngle, 0.0f);
            Quaternion combinedRotation = cachedTransform.rotation * steerRotation;
            wheelForward = combinedRotation * Vector3.forward;
            wheelRight = combinedRotation * Vector3.right;
            wheelUp = combinedRotation * Vector3.up;

            isGrounded = Physics.Raycast(cachedPosition, -wheelUp, out hitResult, suspensionLength + wheelRadius, collisionLayers, QueryTriggerInteraction.Ignore);

            Debug.DrawRay(hitResult.point, wheelForward, Color.blue, 0.0f, false);
            Debug.DrawRay(hitResult.point, wheelRight, Color.red, 0.0f, false);
            Debug.DrawRay(hitResult.point, wheelUp, Color.green, 0.0f, false);

            previousSuspensionLength = currentSuspensionLength;
            currentSuspensionLength = isGrounded ? hitResult.distance - wheelRadius : suspensionLength;
            load = ((suspensionLength - currentSuspensionLength) * springRate) + (((previousSuspensionLength - currentSuspensionLength) / deltaTime) * damperRate);
            load = Mathf.Clamp(load, 0.0f, cachedRigidbody.mass * 9.81f * 2.0f);
            cachedRigidbody.AddForceAtPosition(load * wheelUp, cachedPosition);

            projectedForward = Vector3.Cross(hitResult.normal, -wheelRight);
            projectedRight = Vector3.Cross(hitResult.normal, wheelForward);

            worldVelocity = cachedRigidbody.GetPointVelocity(cachedPosition);
            localVelocity.x = Vector3.Dot(worldVelocity, projectedRight);
            localVelocity.y = Vector3.Dot(worldVelocity, projectedForward);

            // engine/drivetrain torque
            angularVelocity += motorTorque / inertia * deltaTime;

            // friction torque
            float vAngularVelocity = localVelocity.y / wheelRadius;
            if (angularVelocity > vAngularVelocity)
            {
                angularVelocity -= (Mathf.Abs(localTireForce.y) * wheelRadius) / inertia * deltaTime;
                if (angularVelocity < vAngularVelocity) angularVelocity = vAngularVelocity;
            }
            if (angularVelocity < vAngularVelocity)
            {
                angularVelocity += (Mathf.Abs(localTireForce.y) * wheelRadius) / inertia * deltaTime;
                if (angularVelocity > vAngularVelocity) angularVelocity = vAngularVelocity;
            }

            // brake torque
            if (angularVelocity > 0.0f)
            {
                angularVelocity -= Mathf.Abs(brakeTorque) * deltaTime / inertia;
                if (angularVelocity < 0.0f) angularVelocity = 0.0f;
            }
            if (angularVelocity < 0.0f)
            {
                angularVelocity += Mathf.Abs(brakeTorque) * deltaTime / inertia;
                if (angularVelocity > 0.0f) angularVelocity = 0.0f;
            }

            slip.x = -(localVelocity.x);
            slip.y = -(localVelocity.y - angularVelocity * wheelRadius);

            /*
            float dotProduct = Vector3.Dot(-Physics.gravity.normalized, hitResult.normal);
            Vector3 gravitationalForce = -Physics.gravity.normalized * (dotProduct != 0 ? load / dotProduct : 0.0f);
            Vector2 localGravitationalForce = Vector2.zero;
            localGravitationalForce.x = Vector2.Dot(projectedRight, gravitationalForce);
            localGravitationalForce.y = Vector2.Dot(projectedForward, gravitationalForce);
            frictionTireForce = slip * (load / Physics.gravity.magnitude) / deltaTime;
            Vector2 combinedCounterForce = localGravitationalForce + frictionTireForce;
            */

            localTireForce = slip * (load / Physics.gravity.magnitude) / deltaTime;
            //localTireForce = slip * (load / Physics.gravity.magnitude) / deltaTime;
            localTireForce = Vector3.ClampMagnitude(localTireForce, load);

            worldTireForce = projectedForward * localTireForce.y + projectedRight * localTireForce.x;
            cachedRigidbody.AddForceAtPosition(worldTireForce, cachedPosition);

            visualTransform.localPosition = new Vector3(0.0f, -currentSuspensionLength, 0.0f);
            visualRotation = Mathf.Repeat(visualRotation + angularVelocity * Mathf.Rad2Deg * deltaTime, 360.0f);
            visualTransform.localEulerAngles = new Vector3(visualRotation, steerAngle, 0.0f);
        }
    }
}