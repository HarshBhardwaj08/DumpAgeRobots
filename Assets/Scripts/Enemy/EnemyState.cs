using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyState : States
{
    protected Enemy enemy;
    protected EnemyStateMachine stateMachine;
    private string animationName;
    protected float stateTimer;

    public EnemyState(Enemy enemy, EnemyStateMachine stateMachine, string animationName)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
        this.animationName = animationName;
        stateTimer = enemy.stateTimer;
    }
    public virtual void EnterState()
    {
      enemy.animator.SetBool(animationName, true);
    }
    public virtual void UpdateState()
    {
     enemy.stateTimer -= Time.deltaTime;
    }
    public virtual void ExitState() 
    {
        enemy.animator.SetBool(animationName, false);
    }

}
public interface States
{
    public  void EnterState();

    public  void UpdateState();

    public  void ExitState();
    
}