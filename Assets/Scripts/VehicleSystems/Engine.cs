using OVP.Utilities;
using UnityEngine;

namespace OVP.VehicleSystems
{
    public class Engine : MonoBehaviour
    {
        [SerializeField] private float _startFriction = 50.0f; // Starting friction for the engine
        [SerializeField] private float _frictionCoefficient = 0.02f; // Friction coefficient for the engine
        [SerializeField] private float _engineInertia = 0.2f; // Engine inertia
        [SerializeField] private float _engineIdleRPM = 1000.0f; // Engine idle RPM
        [SerializeField] private float _engineMaxRPM = 7500.0f; // Engine max RPM
        [SerializeField] private AnimationCurve _torqueCurve = new AnimationCurve(
            new Keyframe(0.0f, 0.0f),
            new Keyframe(1200.0f, 250.0f, 0.4f, 0.1f),
            new Keyframe(4500.0f, 400.0f),
            new Keyframe(9000.0f, 0.0f)); // Torque curve for the engine

        public float EngineTorque { get; private set; } = 0.0f; // Current engine torque
        public float EngineRPM { get; private set; } = 0.0f; // Current engine RPM
        public float EngineAngularVelocity { get; private set; } = 0.0f; // Current engine angular velocity

        public void UpdatePhysics(float deltaTime, float throttle, float loadTorque)
        {
            EngineAcceleration(deltaTime, throttle, loadTorque);
        }

        private void EngineAcceleration(float deltaTime, float throttle, float loadTorque)
        {
            float maxEffectiveTorque = _torqueCurve.Evaluate(EngineRPM); // Get max effective torque from the torque curve
            float friction = _startFriction + _frictionCoefficient * EngineRPM; // Calculate friction
            float currentInitialTorque = (maxEffectiveTorque + friction) * throttle; // Calculate current initial torque
            EngineTorque = currentInitialTorque - friction; // Calculate engine torque

            // Load torque temporary removed
            float acceleration = (EngineTorque - loadTorque) / _engineInertia; // Calculate engine acceleration (Can change to multiplication for efficiency)
            EngineAngularVelocity += acceleration * deltaTime; // Update engine angular velocity
            EngineAngularVelocity = Mathf.Clamp(EngineAngularVelocity, MathExtensions.RPMToRad(_engineIdleRPM), MathExtensions.RPMToRad(_engineMaxRPM)); // Clamp engine angular velocity within idle and max RPM
            EngineRPM = MathExtensions.RadToRPM(EngineAngularVelocity); // Update engine RPM
        }
    }
}