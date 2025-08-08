using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class EnemyAnimationManagement : MonoBehaviour
{
    public void EnableScreamAnim()
    {
        SignalManager.Instance.Fire(new EnableScreamAnimSignal() { _enabled = true });
    }
}
public class EnableScreamAnimSignal
{
   public bool _enabled;
}