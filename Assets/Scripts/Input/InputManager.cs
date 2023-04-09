using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace OVP.Input
{
    public class InputManager : MonoBehaviour
    {
        // Create a static instance of InputManager that can be accessed from other scripts
        public static InputManager Instance { get; private set; }

        private void Awake()
        {
            // If an instance of InputManager already exists, destroy this duplicate instance
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("InputManager: Duplicate instance detected. Destroying the duplicate instance.");
                Destroy(gameObject);
            }
            else
            {
                // If there is no existing instance, set this as the instance
                Instance = this;
                // Make sure that this object persists across scenes
                DontDestroyOnLoad(gameObject);
            }
        }

        private float _inputThrottle = 0.0f;
        private float _inputBrake = 0.0f;
        private float _inputSteering = 0.0f;

        // Properties to access the input values from other scripts
        public float InputThrottle { get => _inputThrottle; }
        public float InputBrake { get => _inputBrake; }
        public float InputSteering { get => _inputSteering; }

        // Events that can be subscribed to by other scripts
        public event Action InputGearUp = delegate { };
        public event Action InputGearDown = delegate { };

        // Callback methods for input actions
        private void OnThrottle(InputValue value)
        {
            _inputThrottle = value.Get<float>();
        }

        private void OnBrake(InputValue value)
        {
            _inputBrake = value.Get<float>();
        }

        private void OnSteering(InputValue value)
        {
            _inputSteering = value.Get<float>();
        }

        private void OnGearUp()
        {
            InputGearUp.Invoke();
        }

        private void OnGearDown()
        {
            InputGearDown.Invoke();
        }
    }
}