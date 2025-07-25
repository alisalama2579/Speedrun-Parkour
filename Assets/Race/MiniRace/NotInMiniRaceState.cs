using System;
using UnityEngine;
using static TransitionLibrary;

public class NotInMiniRaceState : MonoBehaviour, IState
{
    public event Action OnEnter;
    [SerializeField] private RaceStart raceStart;
    private bool playerInPosition;

    private void Awake()
    {
        raceStart.onPlayerEnter += () => playerInPosition = true;
    }

    public void EnterState(IStateSpecificTransitionData _)
    {
        OnEnter?.Invoke();
    }
    public void InitializeTransitions(IStateMachine stateMachine)
    {
        stateMachine.AddTransition(GetType(), typeof(MiniRacePrepState), ToRacePrepEnter);
    }

    public IStateSpecificTransitionData ToRacePrepEnter()
    {
        if (playerInPosition)
        {
            playerInPosition = false;
            return new SuccesfulTransitionData();
        }
        return failedData;
    }
}