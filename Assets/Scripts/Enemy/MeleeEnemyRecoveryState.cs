using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeEnemyRecoveryState : EnemyState
{
    private bool isChasing = false;
    private MeeleEnemy enemy;
    public MeleeEnemyRecoveryState(Enemy enemy, EnemyStateMachine stateMachine, string animationName) : base(enemy, stateMachine, animationName)
    {
       this.enemy = enemy as MeeleEnemy;
    }

    public override void EnterState()
    {
        base.EnterState();
        SignalManager.Instance.Subscribe<EnableScreamAnimSignal>(OnEnableScreamAnim);
      
    }

    private void OnEnableScreamAnim(EnableScreamAnimSignal signal)
    {
       isChasing = signal._enabled;
    }

    public override void ExitState()
    {
        base.ExitState();
        isChasing = false;
        SignalManager.Instance.Unsubscribe<EnableScreamAnimSignal>(OnEnableScreamAnim);
    }

    public override void UpdateState()
    {
        base.UpdateState();
        if (isChasing == true) {
          
            stateMachine.ChangeState(enemy.chaseMeeleEnemyState);
        }
    }
}
