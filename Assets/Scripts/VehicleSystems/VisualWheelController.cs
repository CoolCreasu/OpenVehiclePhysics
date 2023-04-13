using UnityEngine;

namespace OVP.VehicleSystems
{
    /// <summary>
    /// Controls the visual representation of a wheel based on a CustomWheelCollider.
    /// </summary>
    public class VisualWheelController : MonoBehaviour
    {
        [SerializeField] private WheelColliderV2 _customWheelCollider = default; // Reference to the CustomWheelCollider

        private void Awake()
        {
            if (!_customWheelCollider) // If CustomWheelCollider is not assigned
            {
                Debug.LogWarning("VisualWheelController requires a reference to a CustomWheelCollider to function");
            }
        }

        private void FixedUpdate()
        {
            if (!_customWheelCollider) return; // If CustomWheelCollider is not assigned, return

            _customWheelCollider.GetWorldPose(out Vector3 position, out Quaternion rotation); // Get the world pose of the wheel

            transform.position = position; // Update the position of the visual wheel
            transform.rotation = rotation; // Update the rotation of the visual wheel
        }
    }
}