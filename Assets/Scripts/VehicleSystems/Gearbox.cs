using System.Collections;
using UnityEngine;

namespace OVP.VehicleSystems
{
    public class Gearbox : MonoBehaviour
    {
        [SerializeField] private float _shiftTime = 0.25f; // Shift time duration in seconds
        // index 0 = Reverse, index 1 = Neutral, index 2+ = Forward
        [SerializeField] private float[] _gearRatios = { -3.73f, 0.0f, 3.80f, 2.35f, 1.52f, 1.16f, 0.90f, 0.71f }; // Array of gear ratios
        private int _currentGear = 1; // Current gear index (1-based)
        private int _nextGear = 1; // Next gear index (1-based)
        private bool _inGear = true; // Flag indicating if the gearbox is currently in gear

        public float GetOutputTorque(float torque)
        {
            // Calculates the output torque based on the current gear ratio
            return torque * _gearRatios[_currentGear];
        }

        public float GetInputShaftVelocity(float outputShaftVelocity)
        {
            // Calculates the shaft velocity based on the current gear ratio
            return outputShaftVelocity * _gearRatios[_currentGear];
        }

        public float GetGear()
        {
            // Returns the current gear ratio
            return _gearRatios[_currentGear];
        }

        public void ShiftGearUp()
        {
            // Shifts to the next higher gear if currently in gear and not already in the highest gear
            if (_inGear && (_currentGear < (_gearRatios.Length - 1)))
            {
                if (_currentGear != 1)
                {
                    // If not shifting from neutral, start a coroutine to change gears with delay
                    _nextGear++;
                    StartCoroutine(ChangeGear());
                }
                else
                {
                    // If shifting from neutral, directly change gears
                    _nextGear++;
                    _currentGear = _nextGear;
                }
            }
        }

        public void ShiftGearDown()
        {
            // Shifts to the next lower gear if currently in gear and not already in the lowest gear
            if (_inGear && (_currentGear > 0))
            {
                if (_currentGear != 1)
                {
                    // If not shifting to neutral, start a coroutine to change gears with delay
                    _nextGear--;
                    StartCoroutine(ChangeGear());
                }
                else
                {
                    // If shifting to neutral, directly change gears
                    _nextGear--;
                    _currentGear = _nextGear;
                }
            }
        }

        private IEnumerator ChangeGear()
        {
            // Coroutine to change gears with delay
            _inGear = false; // Set inGear flag to false to indicate gear change in progress
            _currentGear = 1; // Change gear to neutral
            yield return new WaitForSeconds(_shiftTime); // Wait for the specified shift time
            _currentGear = _nextGear; // Change to the next gear
            _inGear = true; // Set inGear flag to true to indicate gear change completed
        }
    }
}