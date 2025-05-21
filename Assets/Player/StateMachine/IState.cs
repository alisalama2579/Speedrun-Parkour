using System.Collections.Generic;
using UnityEngine;
using static TransitionLibrary;

public interface IState
{
    public void EnterState(IStateSpecificTransitionData lastStateData) { }
    public void ExitState() { }
    public void Update(MovementInput input) { }
    public void FixedUpdate() { }
}

public interface IMovementState : IState
{
    public void InitializeTransitions(PlayerStateMachine controller) { }

    public void HandleInput(MovementInput input) { }
    public void CollisionEnter(Collision2D collision) { }
    public void CollisionExit(Collision2D collision) { }
    public void TriggerEnter(Collider2D trigger) { }
    public void TriggerExit(Collider2D trigger) { }

    public void CollisionEnter(IPlayerCollisionInteractor collisionListener) { }
    public void CollisionExit(IPlayerCollisionInteractor collisionListener) { }
    public void TriggerEnter(IPlayerCollisionInteractor collisionListener) { }
    public void TriggerExit(IPlayerCollisionInteractor collisionListener) { }
}

public interface IMovementObserverState<T> : IState where T : IMovementState
{
    public T MovementState { get; set; }
}
