using System;
using System.Collections;
using UnityEngine;
using static TransitionLibrary;

public class MiniRacePrepState : MonoBehaviour, IState
{
    public event Action OnPrepComplete;
    public event Action OnEnter;

    public void EnterState(IStateSpecificTransitionData data)
    {
        OnEnter?.Invoke();
        StartCoroutine(CountDown());
    }
    public void ExitState()
    {
        countdownComplete = false;
    }
    public void InitializeTransitions(IStateMachine machine)
    {
        machine.AddTransition(GetType(), typeof(InMiniRaceState), ToInRaceState);
    }

    private bool PrepComplete() => true;
    private bool countdownComplete;

    private IEnumerator CountDown()
    {
        yield return new WaitUntil(PrepComplete);

        for (int i = 0; i < IRaceController.COUNTDOWNS; i++)
        {
            yield return new WaitForSeconds(1);
            IRaceController.OnCountDown?.Invoke(i);
        }

        countdownComplete = true;
    }

    private IStateSpecificTransitionData ToInRaceState()
    {
        if (countdownComplete) return new SuccesfulTransitionData();
        return failedData;
    }
}
