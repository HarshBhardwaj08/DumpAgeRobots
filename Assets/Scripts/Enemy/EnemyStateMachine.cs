using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStateMachine 
{
    private EnemyState currentState;
    public  void Intialize( EnemyState enterState)
    {
        this.currentState = enterState;
        currentState.EnterState();
    }
    public  void ChangeState(EnemyState exitState)
    {
        currentState.ExitState();
        this.currentState = exitState;
        currentState.EnterState();
    }
    public EnemyState CurrentState()=> currentState;
}
