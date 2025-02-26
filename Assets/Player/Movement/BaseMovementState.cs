using UnityEngine;

public class BaseMovementState
{
    protected Player player;
    protected Rigidbody2D playerRB;
    protected PlayerControls controls;
    protected Collider2D playerCol;

    public BaseMovementState(Player player, ScriptableObject movementStats, PlayerControls controls, Rigidbody2D rb, Collider2D col){}

    public virtual void EnterState(){ }
    public virtual void ExitState() { }

    public virtual void Update() { }
    protected virtual void HandleInput() { }
    public virtual void UpdateMovement() {}

    public virtual void SwitchState(BaseMovementState nextState) { }

    public virtual void PlayerTakeDamage() { }


    public virtual void CollisionEnter(Collision2D collision) { }
    public virtual void CollisionExit(Collision2D collision) { }
    public virtual void TriggerEnter(Collider2D trigger) { }
    public virtual void TriggerExit(Collider2D trigger) { }

    public virtual void CollisionEnter(IPlayerCollisionInteractor collisionListener) { }
    public virtual void CollisionExit(IPlayerCollisionInteractor collisionListener) { }
    public virtual void TriggerEnter(IPlayerCollisionInteractor collisionListener) { }
    public virtual void TriggerExit(IPlayerCollisionInteractor collisionListener) { }
}
