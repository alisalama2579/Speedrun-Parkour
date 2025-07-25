using System;
using UnityEngine;

public class NotInRaceState : MonoBehaviour, IState
{
    public event Action OnEnter;
    public virtual void InitializeTransitions(IStateMachine stateMachine)
    {

    }
}