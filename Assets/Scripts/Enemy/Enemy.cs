using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Enemy : MonoBehaviour
{
    public EnemyStateMachine statemachine { get; private set; }
    public float stateTimer;
    public NavMeshAgent navMeshAgent { get; private set; }
    public Animator animator;
    public Transform[] partolPoints;
    private int currentPartolPointIndex = 0;
    public float turnSpeed = 5f;
    public float aiSpeed = 3f;
    // Controls how fast the enemy rotates
    public float moveSpeedOverride = -1f; // -1 = use NavMeshAgent.speed, else override
  
    public Transform Players;
    public virtual void Awake()
    {
        Players =  FindAnyObjectByType<Player>().transform;
        statemachine = new EnemyStateMachine();
        navMeshAgent = GetComponent<NavMeshAgent>();
    }
    public virtual void Start()
    {
        navMeshAgent.updatePosition = false;
        navMeshAgent.updateRotation = false;
        foreach (Transform point in partolPoints)
        {
            point.parent = null;
        }
       
    }
    public virtual void Update()
    {

    }
    public Vector3 GetDestination()
    {
        Vector3 destination = partolPoints[currentPartolPointIndex].position;
        currentPartolPointIndex = (currentPartolPointIndex + 1) % partolPoints.Length;

        return destination;
    }
    public Quaternion FaceTarget(Vector3 targetPosition)
    {
        Quaternion targetRotation = Quaternion.LookRotation(targetPosition - transform.position);
        Vector3 currentEulerAngles = transform.rotation.eulerAngles;
        float yRotation = Mathf.LerpAngle(currentEulerAngles.y,targetRotation.eulerAngles.y,turnSpeed*Time.deltaTime);
        return Quaternion.Euler(currentEulerAngles.x,yRotation,currentEulerAngles.z);
    }
    private float GetWallDistance(Vector3 direction, float maxDistance)
    {
        if (Physics.Raycast(this.transform.position + Vector3.up * 0.5f, direction, out RaycastHit hit, maxDistance))
        {
            return hit.distance;
        }
        else
        {
            return maxDistance;
        }
    }
    public void SmoothRotateTowards(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position).normalized;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float rotationSpeed = turnSpeed * Time.deltaTime * 100f;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed
        );
    }

    public void MoveForward(float speedMultiplier = 1f)
    {
        float moveSpeed = moveSpeedOverride > 0 ? moveSpeedOverride : navMeshAgent.speed;
        float finalSpeed = moveSpeed * speedMultiplier;

        Vector3 forwardMove = transform.forward * finalSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + forwardMove;

        if (NavMesh.SamplePosition(newPosition, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            transform.position = hit.position;
        }
        else
        {
            transform.position = newPosition; // fallback
        }
    }

}
