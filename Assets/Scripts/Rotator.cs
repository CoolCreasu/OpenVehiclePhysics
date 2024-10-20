using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Rotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 10.0f;

    private Rigidbody cachedRigidbody = default;

    private void OnEnable()
    {
        cachedRigidbody = GetComponent<Rigidbody>();
        if (cachedRigidbody == null)
        {
            enabled = false;
            return;
        }
        cachedRigidbody.useGravity = false;
        cachedRigidbody.isKinematic = true;
    }

    private void FixedUpdate()
    {
        cachedRigidbody.MoveRotation(cachedRigidbody.rotation * (Quaternion.Euler(0, rotationSpeed * Time.fixedDeltaTime, 0)));
    }
}
