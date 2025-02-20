using UnityEngine;

public abstract class PlayerStateMachine : MonoBehaviour
{
    protected BaseMovementState MovementState;

    public void InitializeMovementState(BaseMovementState startingState)
    {
        MovementState = startingState;
        MovementState?.EnterState();
    }

    public void SwitchMovementState(BaseMovementState nextState) 
    { 

    }
}
