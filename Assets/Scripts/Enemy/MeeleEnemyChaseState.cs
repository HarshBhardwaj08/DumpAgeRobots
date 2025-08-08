using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeeleEnemyChaseState : EnemyState
{
    private MeeleEnemy enemy;
    private float lostSightTimer = 0f;
    private float lostSightThreshold = 2f;

    private EnemyFovMesh fov;

    public MeeleEnemyChaseState(Enemy enemy, EnemyStateMachine stateMachine, string animationName)
        : base(enemy, stateMachine, animationName)
    {
        this.enemy = enemy as MeeleEnemy;
    }

    public override void EnterState()
    {
        base.EnterState();
        Debug.Log("Meele Enemy Chase State Entered");

        enemy.moveSpeedOverride = 5f; // fast chase speed
        enemy.animator.applyRootMotion = false; // disable root motion if used
        fov = enemy.GetComponentInChildren<EnemyFovMesh>();
        lostSightTimer = 0f;
    }

    public override void ExitState()
    {
        base.ExitState();
        enemy.moveSpeedOverride = -1f; // reset to default
        enemy.navMeshAgent.speed = enemy.aiSpeed;
    }

    public override void UpdateState()
    {
        base.UpdateState();

        // 1. Handle player lost
        if (fov != null && !fov.PlayerVisible)
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= lostSightThreshold)
            {
                Debug.Log("Player lost for 2+ seconds. Returning to idle.");
                stateMachine.ChangeState(enemy.idleMeeleEnemyState); // or patrol
                return;
            }
        }
        else
        {
            lostSightTimer = 0f;
        }

        // 2. Smooth rotation
        Vector3 toPlayer = (enemy.Players.position - enemy.transform.position).normalized;
        if (toPlayer.sqrMagnitude < 0.01f)
            return;

        enemy.SmoothRotateTowards(enemy.Players.position);

        // 3. Only move forward when mostly facing the player
        float dot = Vector3.Dot(enemy.transform.forward, toPlayer);
        if (dot > 0.95f)
        {
            enemy.MoveForward();
        }

        // 4. Optional: stop at attack distance
        float distanceToPlayer = Vector3.Distance(enemy.transform.position, enemy.Players.position);
        if (distanceToPlayer <= enemy.navMeshAgent.stoppingDistance)
        {
            Debug.Log("Enemy is close enough to attack.");
            // TODO: Transition to attack state
        }
    }
}
