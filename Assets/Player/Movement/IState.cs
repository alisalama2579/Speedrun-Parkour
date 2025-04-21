using System.Collections.Generic;
using UnityEngine;
using static TransitionLibrary;

public interface IState
{
    public void EnterState(IStateSpecificTransitionData lastStateData) { }
    public void ExitState() { }

    public void Update(Player.Input frameInput) { }
    public void FixedUpdate() { }
}


public interface IMovementState : IState
{
    public void InitializeTransitions(PlayerStateMachine controller) { }

    public void HandleInput(Player.Input frameInput) { }
    public void CollisionEnter(Collision2D collision) { }
    public void CollisionExit(Collision2D collision) { }
    public void TriggerEnter(Collider2D trigger) { }
    public void TriggerExit(Collider2D trigger) { }

    public void CollisionEnter(IPlayerCollisionListener collisionListener) { }
    public void CollisionExit(IPlayerCollisionListener collisionListener) { }
    public void TriggerEnter(IPlayerCollisionListener collisionListener) { }
    public void TriggerExit(IPlayerCollisionListener collisionListener) { }
}

public interface IMovementObserverState<T> : IState where T : IMovementState
{
    public T MovementState { get; set; }
}
