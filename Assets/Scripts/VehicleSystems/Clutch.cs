using OVP.Utilities;
using UnityEngine;

namespace OVP.VehicleSystems
{
    public class Clutch : MonoBehaviour
    {
        private float _clutchAngularVelocity = 0.0f;
        private float _engineAngularVelocity = 0.0f;
        private float _clutchAngularAcceleration = 0.0f;
        private float _gearboxRatio = 0.0f;
        private float _clutchSlip = 0.0f;
        private float _clutchLock = 0.0f;
        [SerializeField] private float _clutchStiffness = 30.0f; // 30 Nm / rad/s
        [SerializeField] private float _clutchCapacity = 1.3f;
        [SerializeField] private float _engineMaxTorque = 400.0f; // Max value of the torque curve important!!!
        private float _clutchMaxTorque = 0.0f;
        [SerializeField, Range(0.0f, 0.9f)] private float _clutchDamping = 0.7f;

        public float ClutchTorque { get; private set; } = 0.0f;

        private void Start()
        {
            _clutchMaxTorque = _engineMaxTorque * _clutchCapacity;
        }

        public void UpdatePhysics(float outputShaftVelocity, float engineAngularVelocity, float gearboxRatio)
        {
            _clutchAngularVelocity = outputShaftVelocity;
            _engineAngularVelocity = engineAngularVelocity;
            _gearboxRatio = gearboxRatio;

            GetClutchTorque();
        }

        public void GetClutchTorque()
        {
            // TODO : Change the value veriable with a fitting name

            _clutchSlip = (_engineAngularVelocity - _clutchAngularVelocity) * Mathf.Sign(Mathf.Abs(_gearboxRatio));

            float value = MathExtensions.MapRangeClamped(MathExtensions.RadToRPM(_engineAngularVelocity), 1000.0f, 1300.0f, 0.0f, 1.0f);

            if (_gearboxRatio == 0.0f) value += 1.0f;
            _clutchLock = Mathf.Min(1.0f, value);

            value = _clutchSlip * _clutchLock * _clutchStiffness;

            Mathf.Clamp(value, -_clutchMaxTorque, _clutchMaxTorque);

            value += (ClutchTorque - value) * _clutchDamping;

            ClutchTorque = value;
        }
    }
}