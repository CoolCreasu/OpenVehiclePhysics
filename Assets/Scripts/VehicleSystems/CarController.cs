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

        /// <summary>
        /// Fixed update method that updates physics for the wheels of the vehicle.
        /// </summary>
        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime; // Get the time since last fixed update

            // Update physics for each wheel collider
            _wheelColliderFL.UpdatePhysics(deltaTime);
            _wheelColliderFR.UpdatePhysics(deltaTime);
            _wheelColliderRL.UpdatePhysics(deltaTime);
            _wheelColliderRR.UpdatePhysics(deltaTime);
        }
    }
}