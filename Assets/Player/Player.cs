using System.Collections;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : PlayerStateMachine
{
    private Rigidbody2D rb;
    private Collider2D col;
    private PlayerControls controls;

    [SerializeField] private PlayerLandControllerStats landMovementStats;
    [SerializeField] private PlayerAnimator animator;
    public LandMovement landState { get; private set; }

    private void Awake()
    {
        currentHealth = maxHealth;

        controls = new PlayerControls();
        controls.Enable();

        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        //Initializes land movement state
        landState = new LandMovement(this, landMovementStats, controls, rb, col);
        InitializeMovementState(landState);

        animator.InitializeAnimator();
    }

    //Nulls for faster GC
    private void OnDisable()
    {
        controls.Disable();

        landState = null;
        controls = null;
    }

    private void Update()
    {
        MovementState.Update();
        animator.UpdateAnimator(
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
    [SerializeField] private int maxHealth;
    private int currentHealth;
    public bool isTakingDamage { get; private set; }
    public float hurtDuration;
    public struct HurtValues
    {
        public Vector2 collisionNormal;
    }

    private void OnDamageDealt(HurtValues hurtValues)
    {
        if (isTakingDamage) return;

        currentHealth--;

        if (currentHealth <= 0) OnDeath();
        else StartCoroutine(OnHurt(hurtValues));
    }

    private IEnumerator OnHurt(HurtValues hurtValues)
    {
        EventsManager.Instance.InvokePlayerHurt(hurtValues);
        isTakingDamage = true;
        MovementState.PlayerTakeDamage();

        yield return new WaitForSeconds(hurtDuration); //TODO: Play hurt animation here
        isTakingDamage = false;
    }

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
            if (trigger.TryGetComponent(out IPlayerCollisionInteractor collisionListener)) collisionListener.OnPlayerEnter();
            MovementState.TriggerEnter(trigger);
        }

        //Colliders
        else if (collision != null)
        {
            if (trigger.TryGetComponent(out IPlayerCollisionInteractor collisionListener)) collisionListener.OnPlayerEnter();
            MovementState.CollisionEnter(collision);
        }
    }
    private void ProcessExitCollisions(Collider2D trigger = null, Collision2D collision = null)
    {
        //Triggers
        if (trigger != null)
        {
            if (trigger.TryGetComponent(out IPlayerCollisionInteractor collisionListener)) collisionListener.OnPlayerExit();

            MovementState.TriggerExit(trigger);
        }

        //Colliders
        else if (collision != null)
        {
            if (trigger.TryGetComponent(out IPlayerCollisionInteractor collisionListener)) collisionListener.OnPlayerExit();

            MovementState.CollisionExit(collision);
        }
    }
}
