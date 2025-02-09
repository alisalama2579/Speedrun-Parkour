using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : PlayerStateMachine
{
    [SerializeField] private PlayerLandControllerStats stats;

    private struct Input
    {
        public bool JumpDown;
        public bool JumpHeld;
        public float Move;
    }

    private float PlayerHalfHeight { get { return transform.localScale.y * 0.5f; } }
    private float PlayerHalfWidth { get { return transform.localScale.x * 0.5f; } }
    private PlayerControls controls;
    private Input frameInput;
    private Rigidbody2D rb;

    private void OnValidate() => Initialize();
    private void Awake()
    {
        controls = new PlayerControls();
        controls.Enable();

        rb = GetComponent<Rigidbody2D>();
        Initialize();
    }
    private void OnDisable()
    {
        controls.Disable();
    }
    private void Initialize() { }



    private float time;
    private void Update()
    {
        time += Time.deltaTime;
        HandleInput();
    }

    private void HandleInput()
    {
        frameInput = new Input
        {
            JumpDown = controls.PlayerLand.Jump.WasPressedThisFrame(),
            JumpHeld = controls.PlayerLand.Jump.IsPressed(),
            Move = controls.PlayerLand.Move.ReadValue<float>()
        };

        if (frameInput.JumpDown)
        {
            jumpRequested = true;
            jumpPressedTime = time;
        }
    }

    float fixedDeltaTime;
    private void FixedUpdate()
    {
        fixedDeltaTime = Time.fixedDeltaTime;

        HandleCollisionInteractions();
        HandleJump();
        HandleGravity();
        HandleMomentum();
        HandleHorizontalMovement();
        ApplyMovement();
    }

    private Vector2 groundNormal = Vector2.up;
    private Vector2 groundHorizontal = Vector2.right;

    private Vector2 movementVelocity;
    private Vector2 verticalVelocity;

    private bool isGrounded;

    private bool isOnWall;
    private Vector2 wallNormal;

    #region Collisions

    private bool isSnappedToGround;

    private void HandleCollisionInteractions()
    {
        Vector2 right = Vector2.right * (PlayerHalfWidth - stats.skinWidth);
        Vector2 down = Vector3.down * (PlayerHalfHeight - stats.skinWidth);

        RaycastHit2D rightTopHit = Physics2D.Raycast(rb.position - down + right, groundHorizontal, stats.wallDetectionDistance, stats.groundLayerMask);
        RaycastHit2D rightMiddleHit = Physics2D.Raycast(rb.position + right, groundHorizontal, stats.wallDetectionDistance, stats.groundLayerMask);
        RaycastHit2D rightBottomHit = Physics2D.Raycast(rb.position + down + right, groundHorizontal, stats.wallDetectionDistance, stats.groundLayerMask);

        RaycastHit2D leftTopHit = Physics2D.Raycast(rb.position - down - right, -groundHorizontal, stats.wallDetectionDistance, stats.groundLayerMask);
        RaycastHit2D leftMiddleHit = Physics2D.Raycast(rb.position - right, -groundHorizontal, stats.wallDetectionDistance, stats.groundLayerMask);
        RaycastHit2D leftBottomHit = Physics2D.Raycast(rb.position + down - right, -groundHorizontal, stats.wallDetectionDistance, stats.groundLayerMask);

        HandleGround(
            Physics2D.Raycast(rb.position + down - right, Vector2.down, stats.groundedDistance, stats.groundLayerMask),
            Physics2D.Raycast(rb.position + down + right, Vector2.down, stats.groundedDistance, stats.groundLayerMask),
            Physics2D.Raycast(rb.position + down, Vector2.down, stats.groundedDistance, stats.groundLayerMask),
            Physics2D.Raycast(rb.position + down - right, Vector2.down, stats.groundSnapDistance, stats.groundLayerMask),
            Physics2D.Raycast(rb.position + down + right, Vector2.down, stats.groundSnapDistance, stats.groundLayerMask),
            Physics2D.Raycast(rb.position + down, Vector2.down, stats.groundSnapDistance, stats.groundLayerMask)
        );

        isOnWall = rightTopHit || rightMiddleHit || leftTopHit || leftMiddleHit;
        Vector2 averageWallNormal = (rightTopHit.normal + rightMiddleHit.normal + leftTopHit.normal + leftMiddleHit.normal) * 0.25f;
        wallNormal = Vector2.LerpUnclamped(averageWallNormal, Vector2.up, stats.jumpUpBias);

        HandleCeiling(
            Physics2D.Raycast(rb.position - down - right, Vector2.up, stats.ceilingDistance, stats.groundLayerMask),
            Physics2D.Raycast(rb.position - down + right, Vector2.up, stats.ceilingDistance, stats.groundLayerMask),
            Physics2D.Raycast(rb.position - down, Vector2.up, stats.ceilingDistance, stats.groundLayerMask)
        );
    }

    private void HandleGround(RaycastHit2D bottomLeftHit, RaycastHit2D bottomMiddleHit, RaycastHit2D bottomRightHit, RaycastHit2D bottomLeftSnapHit, RaycastHit2D bottomMiddleSnapHit, RaycastHit2D bottomRightSnapHit)
    {
        Debug.DrawLine(rb.position, bottomLeftHit.point);
        Debug.DrawLine(rb.position, bottomRightHit.point);
        Debug.DrawLine(rb.position, bottomMiddleHit.point);

        bool newGrounded = false;
        isSnappedToGround = false;
        Vector2 averageGroundNormal = (bottomLeftHit.normal + bottomRightHit.normal + bottomMiddleHit.normal).normalized;
        Vector2 averageGroundSnapNormal = (bottomLeftSnapHit.normal + bottomRightSnapHit.normal + bottomMiddleSnapHit.normal).normalized;


        if (bottomLeftHit || bottomRightHit || bottomMiddleHit)
        {
            if(averageGroundNormal.y > stats.minGroundNormal)
            {
                newGrounded = true;
                groundNormal = averageGroundNormal;
                averageGroundSnapNormal = averageGroundNormal;
            }
        }

        if (averageGroundSnapNormal.y > stats.minGroundNormal && newGrounded && !isJumping)
        {
            groundNormal = averageGroundSnapNormal;
            isSnappedToGround = true;
        }

        if (isGrounded ^ newGrounded) OnChangeGrounded(newGrounded);
    }

    private void HandleCeiling(RaycastHit2D topLeftHit, RaycastHit2D topMiddleHit, RaycastHit2D topRightHit)
    {
        Debug.DrawLine(rb.position, topLeftHit.point);
        Debug.DrawLine(rb.position, topRightHit.point);
        Debug.DrawLine(rb.position, topMiddleHit.point);

        Vector2 ceilingNudgeDir = Vector2.zero;
        Vector2 averageCeilNormal = (topLeftHit.normal + topMiddleHit.normal + topRightHit.normal).normalized;

        if (((!topRightHit && topLeftHit) || (!topLeftHit && topRightHit)) && isJumping && averageCeilNormal.y < stats.maxCeilNormal && topMiddleHit)
        {
            if (topLeftHit) ceilingNudgeDir = (topLeftHit.point - rb.position).normalized;
            if (topRightHit) ceilingNudgeDir = (topRightHit.point - rb.position).normalized;

            rb.position += ceilingNudgeDir * frameInput.Move;
        }
        else if (topLeftHit || topMiddleHit || topRightHit)
        {
            verticalVelocity.y = Mathf.Min(averageCeilNormal.y * stats.ceilingHitPush, verticalVelocity.y);
            endedJumpEarly = true;
        }
    }

    #endregion

    #region Ground
    private void OnChangeGrounded(bool newGrounded)
    {
        isGrounded = newGrounded;
        if (isGrounded)
        {
            isJumping = false;
            isGrounded = true;
            coyoteUsable = true;
            bufferedJumpUsable = true;
            endedJumpEarly = false;
        }
        else
        {
            leftGroundTime = time;
            if (!isJumping) endedJumpEarly = true;
        }
    }

    #endregion

    #region Momentum

    private void HandleMomentum()
    {
        if ((movementVelocity.magnitude > stats.momentumGainThreshold || verticalVelocity.magnitude > stats.momentumGainThreshold))
        { momentum += stats.momentumGainSpeed * fixedDeltaTime; }
        else { momentum -= stats.momentumLossSpeed * fixedDeltaTime; }
        momentum = Mathf.Clamp01(momentum);
    }

    #endregion

    #region HorizontalMovement

    private float targetSpeed;
    private float acceleration;
    private float momentum;

    private void HandleHorizontalMovement()
    {
        float moveInput = frameInput.Move;
        float apexBonus = moveInput * stats.apexSpeedMultiplier * ApexPoint;

        targetSpeed = Mathf.LerpUnclamped(stats.minLandSpeed, stats.maxLandSpeed, momentum) * moveInput;
        if (!isGrounded) targetSpeed *= stats.airSpeedMultiplier;
        targetSpeed += apexBonus;

        acceleration = Mathf.LerpUnclamped(stats.minAcceleration, stats.maxAcceleration, momentum);
        if (!isGrounded) acceleration *= stats.airAccelerationMultiplier;

        float speed = Mathf.MoveTowards(movementVelocity.magnitude * moveInput, targetSpeed, acceleration * fixedDeltaTime);
        movementVelocity = speed * groundHorizontal;
    }

    #endregion

    #region Jump

    private bool jumpRequested;
    private bool endedJumpEarly;
    private bool coyoteUsable;
    private bool bufferedJumpUsable;
    private float jumpPressedTime;
    private float leftGroundTime;
    private bool isJumping;


    public float ApexPoint
    {
        get
        {
            if (isJumping) return Mathf.InverseLerp(stats.jumpApexRange, 0, Mathf.Abs(rb.linearVelocityY));
            else return 0;
        }
    }

    private float JumpVelocity => Mathf.Sqrt(2f * stats.gravity * stats.jumpHeight);
    private bool HasBufferedJump => bufferedJumpUsable && time < jumpPressedTime + stats.jumpBuffer;
    private bool CanUseCoyote => coyoteUsable && !isGrounded && time < leftGroundTime + stats.coyoteTime;

    private void HandleJump()
    {
        if (!isGrounded && !frameInput.JumpHeld && rb.linearVelocityY > 0 && isJumping) endedJumpEarly = true;

        if (!jumpRequested && !HasBufferedJump) return;

        if ((isGrounded || CanUseCoyote) && jumpRequested) ExecuteJump();

        jumpRequested = false;
    }

    private void ExecuteJump()
    {
        isJumping = true;
        jumpRequested = false;
        endedJumpEarly = false;
        bufferedJumpUsable = false;
        coyoteUsable = false;
        isSnappedToGround = false;
        verticalVelocity = JumpVelocity * Vector2.LerpUnclamped(groundNormal, Vector2.up, stats.jumpUpBias);
    }

    #endregion

    #region Gravity

    private void HandleGravity()
    {
        if (isGrounded && !isJumping) verticalVelocity = Vector2.zero;
        if (!isGrounded)
        {
            float apexAntiGravity = Mathf.Lerp(0, -stats.apexAntigravity, ApexPoint);
            float inAirGravity = stats.gravity + apexAntiGravity;

            if ((endedJumpEarly && verticalVelocity.y > 0 || verticalVelocity.y < stats.jumpVelocityFallof) && isJumping) inAirGravity *= stats.gravAfterFalloffMultiplier;
            verticalVelocity.y = Mathf.MoveTowards(verticalVelocity.y, -stats.maxDownVelocity, inAirGravity * Time.fixedDeltaTime);
        }
    }

    #endregion

    private void ApplyMovement() 
    {
        if (isSnappedToGround) verticalVelocity = Vector2.down * stats.groundingPush;
        rb.linearVelocity = movementVelocity + verticalVelocity;
    } 
}
