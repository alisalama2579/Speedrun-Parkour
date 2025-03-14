using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerControls controls;
    private MovementStateMachine movementMachine;

    [SerializeField] private PlayerAnimator animator;
    [SerializeField] private MovementStatsHolder movementStats;
    [SerializeField] private bool startInSand;

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Enable();

        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        movementMachine = new MovementStateMachine(startInSand ? typeof(BurrowMovement) : typeof(LandMovement), movementStats, controls, rb, col);

    }

    public struct Input
    {
        public bool DashDown;
        public bool JumpPressed;
        public bool JumpHeld;
        public float HorizontalMove;
        public Vector2 Move;
    }

    private Input frameInput;

    private void HandleInput()
    {
        frameInput = new Input
        {
            JumpPressed = controls.PlayerMovement.Jump.WasPressedThisFrame(),
            JumpHeld = controls.PlayerMovement.Jump.IsPressed(),
            HorizontalMove = controls.PlayerMovement.HorizontalMove.ReadValue<float>(),
            DashDown = controls.PlayerMovement.Dash.WasPressedThisFrame(),
            Move = controls.PlayerMovement.Move.ReadValue<Vector2>(),
        };
    }

    private void OnDisable()
    {
        controls.Disable();

        //Nulls for faster GC
        movementMachine = null;
        controls = null;
    }

    private void Update()
    {
        HandleInput();
        movementMachine?.Update(frameInput);
    }

    private void FixedUpdate()
    {
        movementMachine?.FixedUpdate();
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
                movementMachine?.TriggerEnter(collisionListener);
            }
            movementMachine?.TriggerEnter(trigger);
        }

        //Colliders
        else if (collision != null)
        {
            if (collision.transform.TryGetComponent(out IPlayerCollisionInteractor collisionListener)) 
            {
                if (collisionListener is DamageDealer) OnDeath();

                collisionListener.OnPlayerEnter();
                movementMachine?.CollisionEnter(collisionListener);
            }
            movementMachine?.CollisionEnter(collision);
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
                movementMachine?.TriggerExit(collisionListener);
            }
            movementMachine?.TriggerExit(trigger);
        }

        //Colliders
        else if (collision != null)
        {
            if (collision.transform.TryGetComponent(out IPlayerCollisionInteractor collisionListener)) 
            { 
                collisionListener.OnPlayerExit();
                movementMachine?.CollisionExit(collisionListener);
            }
            movementMachine?.CollisionExit(collision);
        }
    }
}
