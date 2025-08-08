using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MeeleEnemyMoveState : EnemyState
{
    private MeeleEnemy enemy;
    private Vector3 destination;
    private bool isSpotted;

    public MeeleEnemyMoveState(Enemy enemy, EnemyStateMachine stateMachine, string animationName) : base(enemy, stateMachine, animationName)
    {
        this.enemy = enemy as MeeleEnemy;
    }

    public override void EnterState()
    {
        base.EnterState();
        SignalManager.Instance.Subscribe<SpottedAlertSignal>(OnSpotted);

        destination = enemy.GetDestination();
        enemy.navMeshAgent.SetDestination(destination);
        enemy.navMeshAgent.nextPosition = enemy.transform.position;
        enemy.navMeshAgent.isStopped = false;
    }

    public override void ExitState()
    {
        base.ExitState();
        SignalManager.Instance.Unsubscribe<SpottedAlertSignal>(OnSpotted);
        enemy.navMeshAgent.isStopped = true;
        isSpotted = false; // Reset spotted state when exiting
    }

    private void OnSpotted(SpottedAlertSignal signal)
    {
        isSpotted = signal.isSpotted;
    }

    public override void UpdateState()
    {
        base.UpdateState();

        if (isSpotted)
        {
            stateMachine.ChangeState(enemy.recoveryMeeleEnemyState);
            return;
        }

        enemy.navMeshAgent.nextPosition = enemy.transform.position;

        Vector3 nextPoint = GetNextCornerPoint();

        enemy.SmoothRotateTowards(nextPoint);
        enemy.MoveForward(); // actual forward movement

        DrawPathGizmos();

        float distanceToDestination = Vector3.Distance(enemy.transform.position, destination);
        if (distanceToDestination <= enemy.navMeshAgent.stoppingDistance)
        {
            stateMachine.ChangeState(enemy.idleMeeleEnemyState);
        }
    }

    private Vector3 GetNextCornerPoint()
    {
        NavMeshPath path = enemy.navMeshAgent.path;

        if (path.corners.Length < 2)
            return enemy.navMeshAgent.destination;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            if (Vector3.Distance(enemy.transform.position, path.corners[i]) < 1f)
            {
                return path.corners[i + 1];
            }
        }

        return path.corners[1]; // default to next node
    }


   private void DrawPathGizmos()
    {
        NavMeshPath path = enemy.navMeshAgent.path;
        if (path == null || path.corners.Length < 2) return;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.green);
        }

        Debug.DrawRay(enemy.transform.position, enemy.transform.forward * 1f, Color.red); // current forward
    }
}
