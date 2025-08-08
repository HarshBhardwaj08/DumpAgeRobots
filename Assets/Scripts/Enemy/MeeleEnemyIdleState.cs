using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MeeleEnemyIdleState : EnemyState
{
    private MeeleEnemy enemy;
    private Quaternion targetRotation;
    private bool shouldRotate = false;
    private float checkDistance = 5f;

    public MeeleEnemyIdleState(Enemy enemy, EnemyStateMachine stateMachine, string animationName) : base(enemy, stateMachine, animationName)
    {
        this.enemy = enemy as MeeleEnemy;
    }

    public override void EnterState()
    {
        base.EnterState();
        enemy.stateTimer = 1.5f;
       Debug.Log("Meele Enemy Idle State Entered");
        SetTargetRotationAwayFromWall();
    }

    public override void ExitState()
    {
        base.ExitState();
        shouldRotate = false;
        Debug.Log("Meele Enemy Idle State Exited");
    }

    public override void UpdateState()
    {
        base.UpdateState();

        if (shouldRotate)
        {
            float rotationSpeed = enemy.turnSpeed * 100f * Time.deltaTime;
            enemy.transform.rotation = Quaternion.RotateTowards(
                enemy.transform.rotation,
                targetRotation,
                rotationSpeed
            );

            if (Quaternion.Angle(enemy.transform.rotation, targetRotation) < 0.5f)
            {
                shouldRotate = false;
            }
        }

        if (enemy.stateTimer < 0)
        {
            stateMachine.ChangeState(enemy.moveMeeleEnemyState);
        }
    }

    private void SetTargetRotationAwayFromWall()
    {
        Transform transform = enemy.transform;
        Vector3 leftDir = -transform.right;
        Vector3 rightDir = transform.right;

        float leftDist = RaycastWallDistance(transform.position, leftDir);
        float rightDist = RaycastWallDistance(transform.position, rightDir);

        Vector3 rotateDir = (leftDist > rightDist) ? leftDir : rightDir;

        // Get axis of rotation (upwards)
        Vector3 axis = Vector3.up;

        // Apply rotation relative to current forward
        Quaternion rotation = Quaternion.AngleAxis(enemy.rotationAngle, axis);
        Vector3 finalDir = rotation * rotateDir;

        targetRotation = Quaternion.LookRotation(finalDir);
        shouldRotate = true;

        // Debug
        Debug.DrawRay(transform.position, rotateDir * checkDistance, Color.yellow, 2f);
    }

    private float RaycastWallDistance(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, checkDistance))
        {
            return hit.distance;
        }

        return checkDistance;
    }
}
/*public class MeeleEnemyMoveState : EnemyState
{
    private MeeleEnemy enemy;
    private Vector3 destination;

    private List<Vector3> clothoidPath = new List<Vector3>();
    private int clothoidIndex = 0;

    public MeeleEnemyMoveState(Enemy enemy, EnemyStateMachine stateMachine, string animationName) : base(enemy, stateMachine, animationName)
    {
        this.enemy = enemy as MeeleEnemy;
    }

    public override void EnterState()
    {
        base.EnterState();

        destination = enemy.GetDestination();
        enemy.navMeshAgent.SetDestination(destination);
        enemy.navMeshAgent.nextPosition = enemy.transform.position;
        enemy.navMeshAgent.isStopped = false;

        GenerateClothoidPathFromNavMesh();
    }

    public override void ExitState()
    {
        base.ExitState();
        Debug.Log("Meele Enemy Move State Exit");
    }

    public override void UpdateState()
    {
        base.UpdateState();

        if (clothoidPath == null || clothoidPath.Count == 0)
            return;

        Vector3 targetPoint = clothoidPath[clothoidIndex];
        SmoothTurnAndMove(targetPoint);
        DebugDrawClothoid();
        enemy.clothoidPath = clothoidPath;
        if (Vector3.Distance(enemy.transform.position, targetPoint) < 0.3f)
        {
            clothoidIndex++;
            if (clothoidIndex >= clothoidPath.Count)
            {
                // Optional: face player or final direction
                *//* if (enemy.player != null)
                 {
                     Vector3 toPlayer = (enemy.player.position - enemy.transform.position).normalized;
                     if (toPlayer != Vector3.zero)
                         enemy.transform.rotation = Quaternion.LookRotation(toPlayer);
                 }*//*

                stateMachine.ChangeState(enemy.idleMeeleEnemyState);
            }
        }
    }

    private void GenerateClothoidPathFromNavMesh()
    {
        clothoidPath.Clear();
        NavMeshPath navPath = new NavMeshPath();
        enemy.navMeshAgent.CalculatePath(destination, navPath);

        if (navPath.corners.Length >= 2)
        {
            for (int i = 0; i < navPath.corners.Length - 1; i++)
            {
                Vector3 start = navPath.corners[i];
                Vector3 end = navPath.corners[i + 1];
                Vector3 dir = (end - start).normalized;

                List<Vector3> segment = ClothoidGenerator.GenerateClothoid(
                    start,
                    dir,
                    end,
                    step: 0.2f,
                    totalLength: Vector3.Distance(start, end)
                );

                if (i > 0 && segment.Count > 0)
                    segment.RemoveAt(0); // avoid duplicate points

                clothoidPath.AddRange(segment);

            }

            clothoidIndex = 0;
        }
        else
        {
            clothoidPath.Add(destination);
            clothoidIndex = 0;
        }
    }

    private void SmoothTurnAndMove(Vector3 targetPosition)
    {
        Vector3 toTarget = (targetPosition - enemy.transform.position);
        Vector3 direction = toTarget.normalized;

        if (direction.sqrMagnitude < 0.01f)
            return;

        // Calculate dynamic turn speed
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float angle = Quaternion.Angle(enemy.transform.rotation, targetRotation);
        float dynamicTurnSpeed = Mathf.Lerp(60f, 180f, angle / 180f); // degrees per sec

        // Rotate
        enemy.transform.rotation = Quaternion.RotateTowards(
            enemy.transform.rotation,
            targetRotation,
            dynamicTurnSpeed * Time.deltaTime
        );

        // Curvature-based movement slowdown
        float moveSpeed = enemy.moveSpeedOverride > 0 ? enemy.moveSpeedOverride : enemy.navMeshAgent.speed;
        float curveSpeedFactor = Mathf.Clamp01(Vector3.Dot(enemy.transform.forward, direction));
        float adjustedSpeed = moveSpeed * curveSpeedFactor;

        Vector3 newPosition = enemy.transform.position + enemy.transform.forward * adjustedSpeed * Time.deltaTime;

        // Clamp to navmesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(newPosition, out hit, 0.5f, NavMesh.AllAreas))
        {
            enemy.transform.position = hit.position;
        }
        else
        {
            enemy.transform.position = newPosition;
        }

        // Debug direction
        Debug.DrawRay(enemy.transform.position, enemy.transform.forward * 1.5f, Color.red);
        Debug.DrawLine(enemy.transform.position, targetPosition, Color.cyan);
    }

    private void DebugDrawClothoid()
    {
        for (int i = 0; i < clothoidPath.Count - 1; i++)
        {
            // Draw lines between clothoid points (curve)
            Debug.DrawLine(clothoidPath[i], clothoidPath[i + 1], Color.yellow);

            // Draw a small vertical ray at each point to show as a dot
            Debug.DrawRay(clothoidPath[i], Vector3.up * 0.2f, Color.green);
        }

        // Also draw final point to confirm end of path
        if (clothoidPath.Count > 0)
        {
            Debug.DrawRay(clothoidPath[^1], Vector3.up * 0.2f, Color.red); // Red = endpoint
        }
    }
}*/
