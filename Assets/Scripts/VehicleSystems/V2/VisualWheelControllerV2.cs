using UnityEngine;

namespace OVP.VehicleSystems
{
    public class VisualWheelControllerV2 : MonoBehaviour
    {
        [SerializeField] private WheelColliderV2 _wheelCollider = default;

        private void Awake()
        {
            if (!_wheelCollider)
            {
                Debug.LogWarning("VisualWheelControllerV2 requires a reference to a WheelColliderV2 to function.");
            }
        }

        private void FixedUpdate()
        {
            if (!_wheelCollider) return;

            _wheelCollider.GetWorldPose(out Vector3 position);

            transform.position = position;
        }
    }
}