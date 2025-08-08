using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Testing : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float switchAngle = 100f; // Angle to switch between front/back
    [SerializeField] private float xLerpSpeed = 10f;   // Speed for X tilt transition

    private float currentX = -55f;

    private void LateUpdate()
    {
        if (target == null) return;

        // Step 1: Get horizontal direction to target
        Vector3 direction = target.position - transform.position;
        direction.y = 0;

        if (direction == Vector3.zero) return;

        // Step 2: Smooth Y-axis rotation
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        Quaternion smoothRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
   
        // Step 3: Get angle between forward and target
        Vector3 toPlayer = (target.position - transform.position).normalized;
        float angleToPlayer = Vector3.Angle(transform.forward, toPlayer);

        // Step 4: Smooth X tilt based on front/back
        if (angleToPlayer < switchAngle)
        {
            currentX = Mathf.Lerp(currentX, -55f, Time.deltaTime * xLerpSpeed); // In front
        }
        else
        {
            currentX = Mathf.Lerp(currentX, 55f, Time.deltaTime * xLerpSpeed); // Behind
        }

        // Step 5: Apply final rotation with fixed Z
        Vector3 finalEuler = smoothRotation.eulerAngles;
        finalEuler.x = currentX;
        finalEuler.z = 0f;
        transform.rotation = Quaternion.Euler(finalEuler);
    }
}
