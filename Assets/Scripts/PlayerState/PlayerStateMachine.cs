using UnityEngine;

public abstract class PlayerStateMachine : MonoBehaviour
{
    protected PlayerState State;

    public void SetState(PlayerState newState)
    {
        State?.ExitState();
        State = newState;
        State?.EnterState();
    }
}
