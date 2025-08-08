using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeeleEnemy : Enemy
{
    public MeeleEnemyIdleState idleMeeleEnemyState { get; private set; }
    public MeeleEnemyMoveState moveMeeleEnemyState { get; private set; }
    public MeleeEnemyRecoveryState recoveryMeeleEnemyState { get; private set; }
    public MeeleEnemyChaseState chaseMeeleEnemyState { get; private set; }
    [Header("Rotation Settings")]
    public float rotationAngle = 180f;
   
    public override void Awake()
    {
        base.Awake();
        idleMeeleEnemyState = new MeeleEnemyIdleState(this,statemachine,"Idle");
        moveMeeleEnemyState = new MeeleEnemyMoveState(this, statemachine, "Move");
        recoveryMeeleEnemyState = new MeleeEnemyRecoveryState(this, statemachine, "Scream");
        chaseMeeleEnemyState = new MeeleEnemyChaseState(this, statemachine, "Chase");
    }

    public override void Start()
    {
        base.Start();
        statemachine.Intialize(idleMeeleEnemyState);
    }

    public override void Update()
    {
        base.Update();
        statemachine.CurrentState().UpdateState();
    }
}
