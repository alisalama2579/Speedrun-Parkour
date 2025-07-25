using System;
using UnityEngine;
using static TransitionLibrary;

public class MiniObjectiveCompletedState : MonoBehaviour, IState
{
    public event Action OnEnter;
    protected bool completedPostRace;

    public virtual void EnterState(IStateSpecificTransitionData data)
    {
        OnEnter?.Invoke();
        completedPostRace = true;

    }
    public virtual void ExitState()
    {
        completedPostRace = false;
    }
    public virtual void InitializeTransitions(IStateMachine machine)
    {
        machine.AddTransition(GetType(), typeof(NotInMiniRaceState), ToNotInRaceState);
    }


    protected virtual IStateSpecificTransitionData ToNotInRaceState()
    {
        if (completedPostRace) return new SuccesfulTransitionData();
        return failedData;
    }

}
