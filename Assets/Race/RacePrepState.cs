using System;
using System.Collections;
using UnityEngine;
using static TransitionLibrary;

public class RacePrepState : MonoBehaviour, IState
{
    public event Action OnPrepComplete;
    public event Action OnEnter;

    public virtual void EnterState(IStateSpecificTransitionData data)
    {
        OnEnter?.Invoke();
    }
    public virtual void ExitState()
    {
        countdownComplete = false;
    }
    public virtual void InitializeTransitions(IStateMachine machine)
    {
        machine.AddTransition(GetType(), typeof(InRaceState), ToInRaceState);
    }

    protected virtual bool PrepComplete() => true;
    protected bool countdownComplete;

    protected virtual IEnumerator CountDown()
    {
        yield return new WaitUntil(PrepComplete);

        for (int i = 0; i < IRaceController.COUNTDOWNS; i++)
        {
            yield return new WaitForSeconds(1);
            IRaceController.OnCountDown?.Invoke(i);
        }

        countdownComplete  = true;
    }

    protected virtual IStateSpecificTransitionData ToInRaceState()
    {
        if (countdownComplete) return new SuccesfulTransitionData();
        return failedData;
    }
}
