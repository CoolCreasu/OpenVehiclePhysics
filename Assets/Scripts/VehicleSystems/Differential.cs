using UnityEngine;

namespace OVP.VehicleSystems
{
    public class Differential : MonoBehaviour
    {
        [SerializeField] private float m_DifferentialGearRatio = 3.9f; // Gear ratio of the differential

        // Get output torque for the left output shaft based on the input torque
        public float GetOutputTorqueLeft(float inputTorque)
        {
            // Symmetrical open differential
            return inputTorque * m_DifferentialGearRatio * 0.5f;
        }

        // Get output torque for the right output shaft based on the input torque
        public float GetOutputTorqueRight(float inputTorque)
        {
            // Symmetrical open differential
            return inputTorque * m_DifferentialGearRatio * 0.5f;
        }

        // Calculate the input shaft velocity based on the output shaft velocities of left and right sides
        public float GetInputShaftVelocity(float outputShaftVelocityLeft, float outputShaftVelocityRight)
        {
            // Input shaft velocity is the sum of left and right output shaft velocities
            // multiplied by half the differential gear ratio to account for distribution of torque
            return outputShaftVelocityLeft + outputShaftVelocityRight * 0.5f * m_DifferentialGearRatio;
        }

        public void GetOutputTorque(float inputTorque, float angularVelocityLeft, float angularVelocityRight, float inertia, float deltaTime, out float outputTorqueLeft, out float outputTorqueRight)
        {
            float symOpenDiff = inputTorque * m_DifferentialGearRatio * 0.5f;
            float lockTorque = (angularVelocityLeft - angularVelocityRight) * 0.5f / deltaTime * inertia;

            outputTorqueLeft = symOpenDiff - lockTorque;
            outputTorqueRight = symOpenDiff + lockTorque;
        }
    }
}