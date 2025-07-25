using System;
using UnityEngine;
using static TransitionLibrary;

public class InMiniRaceState : MonoBehaviour, IState
{
    [SerializeField] private RaceEnd racEnd;
    private bool raceObjectiveMet;
    public event Action OnEnter;

    private void Awake()
    {
        racEnd.onPlayerEnter += () => raceObjectiveMet = true;
    }

    public void EnterState(IStateSpecificTransitionData data)
    {
        OnEnter?.Invoke();
    }
    public void ExitState()
    {
        raceObjectiveMet = false;
    }
    public virtual void InitializeTransitions(IStateMachine machine)
    {
        machine.AddTransition(GetType(), typeof(MiniObjectiveCompletedState), ToObjectiveCompletedState);
    }

    protected virtual IStateSpecificTransitionData ToObjectiveCompletedState()
    {
        if (raceObjectiveMet) return new SuccesfulTransitionData();
        return failedData;
    }
}
