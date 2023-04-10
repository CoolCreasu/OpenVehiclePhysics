using UnityEngine;

namespace OVP.VehicleSystems
{
    /// <summary>
    /// Car controller that updates physics for the wheels of a vehicle.
    /// </summary>
    /// 
    public class VehicleInput
    {
        private float _steerInput;
        private float _accelerationInput;
        public float SteerInput
        {
            get => _steerInput;
            set => _steerInput = Mathf.Clamp(value,-1f,1f);
        }

        public float AccelerationInput
        {
            get => _accelerationInput;
            set => _accelerationInput = Mathf.Clamp(value, -1f, 1f);
        }

        public void Reset()
        {
            _steerInput = 0;
            _accelerationInput = 0;
        }
    }

    public class CarController : MonoBehaviour
    {
        [SerializeField] private bool controlNow = false;
        [SerializeField] private CustomWheelCollider wheelColliderFL = default; // Front left wheel collider
        [SerializeField] private CustomWheelCollider wheelColliderFR = default; // Front right wheel collider
        [SerializeField] private CustomWheelCollider wheelColliderRL = default; // Rear left wheel collider
        [SerializeField] private CustomWheelCollider wheelColliderRR = default; // Rear right wheel collider

        [SerializeField] private float maxSteeringAngle = 45f; //Max Steering angle in Degrees
        [SerializeField] private float maxSpeed = 50; // Max speed in M/S
        [SerializeField] private float reverseMaxSpeed = 20; // Max speed in M/S
        [SerializeField] private AnimationCurve torqueSpeedCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)); //torque curve by speed
        [SerializeField] private AnimationCurve reverseTorqueCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0)); //reverse curve by speed
        [SerializeField] private float engineTorque = 1000f; //Engine torque
        [SerializeField] private float brakeTorque = 4000f; //Brake torque

        private Vector3 _worldVelocity; //World velocity
        private Vector3 _localVelocity; //Local velocity

        private Rigidbody _vehicleRigidbody; //rigidbody of vehicle

        private VehicleInput _input; //input class

        private bool _isReverse; //Is it reverse?

        /// <summary>
        /// Start function of the vehicle
        /// </summary>
        private void Start()
        {
            _input = new VehicleInput();

            _vehicleRigidbody = gameObject.GetComponent<Rigidbody>();
            if(_vehicleRigidbody == null)
            {
                Debug.LogError(gameObject.name + "doesn`t contain rigidbody component");
            }
        }


        /// <summary>
        /// Fixed update method that updates physics of the vehicle.
        /// </summary>
        private void FixedUpdate()
        {
            float fixedDeltaTime = Time.fixedDeltaTime; // Get the time since last fixed update
            HandleInput();
            CalculateVelocities();
            RunVehicleLogic();

            // Update physics for each wheel collider
            wheelColliderFL.UpdatePhysics(fixedDeltaTime);
            wheelColliderFR.UpdatePhysics(fixedDeltaTime);
            wheelColliderRL.UpdatePhysics(fixedDeltaTime);
            wheelColliderRR.UpdatePhysics(fixedDeltaTime);
        }
        /// <summary>
        /// Calculates the velocities
        /// </summary>
        private void CalculateVelocities()
        {
            //Getting world and local velocity
            _worldVelocity = _vehicleRigidbody.velocity;
            _localVelocity = transform.InverseTransformDirection(_worldVelocity);

        }
        /// <summary>
        /// Handle the input
        /// </summary>
        private void HandleInput() //Getting input for the vehicle
        {
            if (controlNow)
            {
                _input.AccelerationInput = Input.GetAxis("Vertical");
                _input.SteerInput = Input.GetAxis("Horizontal");
            }
            else
            {
                _input.Reset();
            }
        }

        /// <summary>
        /// Handle the motor, brakes, steering
        /// </summary>
        private void RunVehicleLogic()
        {
            //Check for reverse input
            _isReverse = (_input.AccelerationInput < 0) ? true : false;

            //Get Forward velocity
            float forwardVelocity = _localVelocity.z;

            //Calculate the percentage of maximum speed
            float velocityPercentage = Mathf.Clamp01(forwardVelocity / ((_isReverse) ? -Mathf.Abs(reverseMaxSpeed) : maxSpeed));
            //Evaluate the torque curve based on velocity percentage
            //The torque curve determines the amount of torque to apply to the wheels based on the current velocity percentage
            float motorMultiplier = (!_isReverse) ? torqueSpeedCurve.Evaluate(velocityPercentage) : reverseTorqueCurve.Evaluate(velocityPercentage);

            //FIX ME: Add some maybe array class for each wheel which can contains condition like Steering, IsMotor, CanBrake etc.
            //Or just add engine, transmission and so on
            wheelColliderFL.DriveTorque = engineTorque * motorMultiplier * _input.AccelerationInput;
            wheelColliderFR.DriveTorque = engineTorque * motorMultiplier * _input.AccelerationInput;
            wheelColliderRL.DriveTorque = engineTorque * motorMultiplier * _input.AccelerationInput;
            wheelColliderRR.DriveTorque = engineTorque * motorMultiplier * _input.AccelerationInput;

            bool braking = false;
            //The first section of code determines whether the vehicle is braking or not.
            //If the vehicle is accelerating in the opposite direction of its current velocity,
            //then the variable "braking" is set to true. Otherwise, it is set to false.
            if ((_input.AccelerationInput < 0 && forwardVelocity > 0) || (_input.AccelerationInput > 0 && forwardVelocity < 0)) braking = true;

            if (braking)
            {
                wheelColliderFR.BrakeTorque = wheelColliderFL.BrakeTorque = wheelColliderRL.BrakeTorque = wheelColliderRR.BrakeTorque = brakeTorque * Mathf.Abs(_input.AccelerationInput);
            }
            else
            {
                wheelColliderFR.BrakeTorque = wheelColliderFL.BrakeTorque = wheelColliderRL.BrakeTorque = wheelColliderRR.BrakeTorque = 0;
            }

            //e sets the steering angle of the front wheels based on the player's steering input.
            //The steering input is multiplied by the maximum steering angle
            //to determine the actual steering angle of the front wheels.
            wheelColliderFL.SteerAngle = _input.SteerInput * maxSteeringAngle;
            wheelColliderFR.SteerAngle = _input.SteerInput * maxSteeringAngle;

        }

    }
}