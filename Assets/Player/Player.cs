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
        animator.UpdateAnimator
(
new PlayerAnimator.AnimationValues
{
    isGrounded = landState.isGrounded,
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
}
