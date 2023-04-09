using OVP.Input;
using UnityEngine;

namespace OVP.VehicleSystems
{
    /// <summary>
    /// Car controller that updates physics for the wheels of a vehicle.
    /// </summary>
    public class CarController : MonoBehaviour
    {
        [SerializeField] private CustomWheelCollider _wheelColliderFL = default; // Front left wheel collider
        [SerializeField] private CustomWheelCollider _wheelColliderFR = default; // Front right wheel collider
        [SerializeField] private CustomWheelCollider _wheelColliderRL = default; // Rear left wheel collider
        [SerializeField] private CustomWheelCollider _wheelColliderRR = default; // Rear right wheel collider

        [SerializeField] private Engine _engine = default;
        [SerializeField] private Clutch _clutch = default;
        [SerializeField] private Gearbox _gearbox = default;
        [SerializeField] private Differential _differential = default;

        [SerializeField] private float _wheelBase = 3.12f;
        [SerializeField] private float _trackWidth = 1.5f;
        [SerializeField] private float _maxSteeringAngle = 40.0f;

        private float _steeringvalue = 0.0f;

        private void Awake()
        {
            if (!_engine) Debug.LogWarning("CarController requires an attached Engine to function.");
            if (!_clutch) Debug.LogWarning("CarController requires an attached Clutch to function.");
            if (!_gearbox) Debug.LogWarning("CarController requires an attached Gearbox to function.");
            if (!_differential) Debug.LogWarning("CarController requires an attached Differential to function.");
        }

        private void OnEnable()
        {
            // In Unity game engine, the Awake() and OnEnable() methods are called immediately one after the other, per object, not per method.
            // This means that when the scripts are initialized, both Awake() and OnEnable() methods are invoked in sequence for each object.
            // In this case, if there is an instance of InputManager available, the InputGearUp and InputGearDown events are subscribed to using the corresponding event handlers, OnGearUp() and OnGearDown().
            // This is the reason we have a null check here.
            if (InputManager.Instance)
            {
                InputManager.Instance.InputGearUp += OnGearUp;
                InputManager.Instance.InputGearDown += OnGearDown;
            }
        }

        private void Start()
        {
            InputManager.Instance.InputGearUp += OnGearUp;
            InputManager.Instance.InputGearDown += OnGearDown;
        }

        private void OnDisable()
        {
            InputManager.Instance.InputGearUp -= OnGearUp;
            InputManager.Instance.InputGearDown -= OnGearDown;
        }

        private void Update()
        {
            _steeringvalue = Mathf.MoveTowards(_steeringvalue, InputManager.Instance.InputSteering * _maxSteeringAngle, Time.deltaTime * 200.0f);
        }

        /// <summary>
        /// Fixed update method that updates physics for the wheels of the vehicle.
        /// </summary>
        private void FixedUpdate()
        {
            if (!_engine || !_clutch || !_gearbox || !_differential) return;

            // BrakeTorque
            _wheelColliderFL.BrakeTorque = 8000 * InputManager.Instance.InputBrake;
            _wheelColliderFR.BrakeTorque = 8000 * InputManager.Instance.InputBrake;
            _wheelColliderRL.BrakeTorque = 8000 * InputManager.Instance.InputBrake;
            _wheelColliderRR.BrakeTorque = 8000 * InputManager.Instance.InputBrake;

            _wheelColliderFL.transform.localRotation = Quaternion.Euler(0.0f, _steeringvalue * (_wheelBase / (_wheelBase + _trackWidth *  Mathf.Sign(_steeringvalue) * 0.5f)), 0.0f);
            _wheelColliderFR.transform.localRotation = Quaternion.Euler(0.0f, _steeringvalue * (_wheelBase / (_wheelBase + _trackWidth * -Mathf.Sign(_steeringvalue) * 0.5f)), 0.0f);

            float deltaTime = Time.fixedDeltaTime; // Get the time since last fixed update

            // Torque stream
            _engine.UpdatePhysics(deltaTime, InputManager.Instance.InputThrottle, _clutch.ClutchTorque);
            float value = _gearbox.GetOutputTorque(_clutch.ClutchTorque);
            _wheelColliderRL.DriveTorque = _differential.GetOutputTorqueLeft(value);
            _wheelColliderRR.DriveTorque = _differential.GetOutputTorqueRight(value);

            // Update physics for each wheel collider
            _wheelColliderFL.UpdatePhysics(deltaTime);
            _wheelColliderFR.UpdatePhysics(deltaTime);
            _wheelColliderRL.UpdatePhysics(deltaTime);
            _wheelColliderRR.UpdatePhysics(deltaTime);

            // Velocity stream
            value = _differential.GetInputShaftVelocity(_wheelColliderRL.WheelAngularVelocity, _wheelColliderRR.WheelAngularVelocity);
            value = _gearbox.GetInputShaftVelocity(value);
            _clutch.UpdatePhysics(value, _engine.EngineAngularVelocity, _gearbox.GetGear());
            _engine.UpdatePhysics(deltaTime, InputManager.Instance.InputThrottle, _clutch.ClutchTorque);
        }

        private void OnGearUp()
        {
            _gearbox.ShiftGearUp();
        }

        private void OnGearDown()
        {
            _gearbox.ShiftGearDown();
        }
    }
}