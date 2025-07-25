using System;
using UnityEngine;
using static TransitionLibrary;

public class InRaceState : MonoBehaviour, IState
{
    protected bool raceObjectiveMet;
    [HideInInspector] public double timeRaceStarted;
    public event Action OnEnter;

    protected virtual void Awake()
    {
        
    }
    protected virtual void EnterState(IStateSpecificTransitionData data)
    {
        OnEnter?.Invoke();
    }
    protected virtual void ExitState()
    {
        raceObjectiveMet = false;
        timeRaceStarted = Time.timeAsDouble;
    }
    public virtual void InitializeTransitions(IStateMachine machine)
    {
        machine.AddTransition(GetType(), typeof(ObjectiveCompletedState), ToObjectiveCompletedState);
    }

    protected virtual IStateSpecificTransitionData ToObjectiveCompletedState()
    {
        if (raceObjectiveMet) return new SuccesfulTransitionData();
        return failedData;
    }
}
