using OVP.Input;
using TMPro;
using UnityEngine;

namespace OVP.VehicleSystems
{
    public class CarControllerV2 : MonoBehaviour
    {
        [SerializeField] private bool _debug = false;
        [SerializeField] private TextMeshProUGUI _debugText = default;

        [SerializeField] private WheelColliderV2 _wheelColliderFL = default;
        [SerializeField] private WheelColliderV2 _wheelColliderFR = default;
        [SerializeField] private WheelColliderV2 _wheelColliderRL = default;
        [SerializeField] private WheelColliderV2 _wheelColliderRR = default;

        [SerializeField] private float _maxSteeringAngle = 40.0f;

        private float _steeringValue = 0.0f;

        private void Start()
        {
            _wheelColliderFL.DebugMode = _debug;
            _wheelColliderFR.DebugMode = _debug;
            _wheelColliderRL.DebugMode = _debug;
            _wheelColliderRR.DebugMode = _debug;
        }

        private void Update()
        {
            // Input
            _steeringValue = Mathf.MoveTowards(_steeringValue, InputManager.Instance.InputSteering * _maxSteeringAngle, Time.deltaTime * 200.0f);

            // Debug
            if (_debug)
            {
                _debugText.text =
                    $"Steer Angle L => {_wheelColliderFL.SteerAngle}\n" +
                    $"Steer Angle R => {_wheelColliderFR.SteerAngle}\n";
            }
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            float deltaTimeInverted = 1.0f / deltaTime;

            _wheelColliderFL.SteerAngle = _steeringValue;
            _wheelColliderFR.SteerAngle = _steeringValue;

            _wheelColliderRL.DriveTorque = 1000.0f * InputManager.Instance.InputThrottle;
            _wheelColliderRR.DriveTorque = 1000.0f * InputManager.Instance.InputThrottle;

            _wheelColliderRL.BrakeTorque = 8000.0f * InputManager.Instance.InputBrake;
            _wheelColliderRR.BrakeTorque = 8000.0f * InputManager.Instance.InputBrake;
            
            _wheelColliderFL.UpdatePhysics(deltaTime, deltaTimeInverted);
            _wheelColliderFR.UpdatePhysics(deltaTime, deltaTimeInverted);
            _wheelColliderRL.UpdatePhysics(deltaTime, deltaTimeInverted);
            _wheelColliderRR.UpdatePhysics(deltaTime, deltaTimeInverted);
        }
    }
}