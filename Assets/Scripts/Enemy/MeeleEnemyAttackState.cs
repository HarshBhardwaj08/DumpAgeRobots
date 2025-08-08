using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeeleEnemyAttackState : EnemyState
{
    public MeeleEnemyAttackState(Enemy enemy, EnemyStateMachine stateMachine, string animationName) : base(enemy, stateMachine, animationName)
    {
    }

    public override void EnterState()
    {
        base.EnterState();
    }

    public override void ExitState()
    {
        base.ExitState();
    }

    public override void UpdateState()
    {
        base.UpdateState();
    }
}
