using UnityEngine;
using static TransitionLibrary;

public interface IState
{
    public abstract void EnterState(IStateSpecificTransitionData lastStateData);
    public abstract void ExitState();

    public void InitializeTransitions(MovementStateMachine controller);
    public abstract void Update(Player.Input frameInput);
    public abstract void HandleInput(Player.Input frameInput);
    public abstract void UpdateMovement();

    public virtual void CollisionEnter(Collision2D collision) { }
    public virtual void CollisionExit(Collision2D collision) { }
    public virtual void TriggerEnter(Collider2D trigger) { }
    public virtual void TriggerExit(Collider2D trigger) { }

    public virtual void CollisionEnter(IPlayerCollisionListener collisionListener) { }
    public virtual void CollisionExit(IPlayerCollisionListener collisionListener) { }
    public virtual void TriggerEnter(IPlayerCollisionListener collisionListener) { }
    public virtual void TriggerExit(IPlayerCollisionListener collisionListener) { }
}
