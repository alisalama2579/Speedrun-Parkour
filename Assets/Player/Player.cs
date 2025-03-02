using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : PlayerStateMachine
{
    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerControls controls;

    [SerializeField] private PlayerLandControllerStats landMovementStats;
    [SerializeField] private PlayerBurrowMovementStats burrowMovementStats;
    [SerializeField] private PlayerAnimator animator;
    public LandMovement landState { get; private set; }
    public BurrowMovement burrowState { get; private set; }

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Enable();

        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        //Initializes movement states
        landState = new LandMovement(this, landMovementStats, controls, rb, col);
        burrowState = new BurrowMovement(this, burrowMovementStats, controls, rb, col);
        InitializeMovementState(burrowState);

        if(animator != null)animator.InitializeAnimator();
    }

    //Nulls for faster GC
    private void OnDisable()
    {
        controls.Disable();

        landState = null;
        burrowState = null;
        controls = null;
    }

    private void Update()
    {
        MovementState.Update();
        if(animator != null) animator.UpdateAnimator(
            new PlayerAnimator.AnimationValues
            {
                isGrounded = landState.IsGrounded,
                isOnWall = landState.isOnWall,
                moveInput = landState.frameInput.Move,
                velocity = landState.velocity,
            }
            );
    }

    private void FixedUpdate()
    {
        MovementState.UpdateMovement();
    }


    //Damage
    private void OnDeath()
    {
        EventsManager.Instance.InvokePlayerDeath();
    }

    //Collisions
    private void OnCollisionEnter2D(Collision2D collision) => ProcessEntryCollisions(collision: collision);
    private void OnTriggerEnter2D(Collider2D trigger) => ProcessEntryCollisions(trigger: trigger);
    private void OnCollisionExit2D(Collision2D collision) => ProcessExitCollisions(collision: collision);
    private void OnTriggerExit2D(Collider2D trigger) => ProcessExitCollisions(trigger: trigger);

    private void ProcessEntryCollisions(Collider2D trigger = null, Collision2D collision = null)
    {
        //Triggers
        if (trigger != null)
        {
            if (trigger.TryGetComponent(out IPlayerCollisionInteractor collisionListener)) 
            {
                if (collisionListener is DamageDealer) OnDeath();

                collisionListener.OnPlayerEnter();
                MovementState.TriggerEnter(collisionListener);
            }
            MovementState.TriggerEnter(trigger);
        }

        //Colliders
        else if (collision != null)
        {
            if (collision.transform.TryGetComponent(out IPlayerCollisionInteractor collisionListener)) 
            {
                if (collisionListener is DamageDealer) OnDeath();

                collisionListener.OnPlayerEnter();
                MovementState.CollisionEnter(collisionListener);
            }
            MovementState.CollisionEnter(collision);
        }
    }
    private void ProcessExitCollisions(Collider2D trigger = null, Collision2D collision = null)
    {
        //Triggers
        if (trigger != null)
        {
            if (trigger.TryGetComponent(out IPlayerCollisionInteractor collisionListener))
            { 
                collisionListener.OnPlayerExit();
                MovementState.TriggerExit(collisionListener);
            }
            MovementState.TriggerExit(trigger);
        }

        //Colliders
        else if (collision != null)
        {
            if (collision.transform.TryGetComponent(out IPlayerCollisionInteractor collisionListener)) 
            { 
                collisionListener.OnPlayerExit();
                MovementState.CollisionExit(collisionListener);
            }
            MovementState.CollisionExit(collision);
        }
    }
}
