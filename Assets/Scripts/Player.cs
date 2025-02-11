using UnityEngine;
using UnityEngine.Assertions.Must;

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

   
    
    #region Collisions

    int wallDir;
    private Vector2 jumpNormal;

    private void HandleCollisionInteractions()
    {
        Vector2 right = Vector2.right * (PlayerHalfWidth - stats.skinWidth);
        Vector2 down = Vector3.down * (PlayerHalfHeight - stats.skinWidth);

        HandleWall(
Physics2D.Raycast(rb.position - down + right, Vector2.right, stats.wallDetectionDistance, stats.collisionLayerMask),
Physics2D.Raycast(rb.position + right, Vector2.right, stats.wallDetectionDistance, stats.collisionLayerMask),
Physics2D.Raycast(rb.position + down + right, Vector2.right, stats.wallDetectionDistance, stats.collisionLayerMask),
Physics2D.Raycast(rb.position - down - right, Vector2.left, stats.wallDetectionDistance, stats.collisionLayerMask),
Physics2D.Raycast(rb.position - right, Vector2.left, stats.wallDetectionDistance, stats.collisionLayerMask),
Physics2D.Raycast(rb.position + down - right, Vector2.left, stats.wallDetectionDistance, stats.collisionLayerMask)
);

        HandleGround(
            Physics2D.Raycast(rb.position + down - right, Vector2.down, stats.groundedDistance, stats.collisionLayerMask),
            Physics2D.Raycast(rb.position + down + right, Vector2.down, stats.groundedDistance, stats.collisionLayerMask),
            Physics2D.Raycast(rb.position + down, Vector2.down, stats.groundedDistance, stats.collisionLayerMask)
        );


        HandleCeiling(
            Physics2D.Raycast(rb.position - down - right, Vector2.up, stats.ceilingDistance, stats.collisionLayerMask),
            Physics2D.Raycast(rb.position - down + right, Vector2.up, stats.ceilingDistance, stats.collisionLayerMask),
            Physics2D.Raycast(rb.position - down, Vector2.up, stats.ceilingDistance, stats.collisionLayerMask)
        );
    }

    private void HandleGround(RaycastHit2D bottomLeftHit, RaycastHit2D bottomMiddleHit, RaycastHit2D bottomRightHit)
    {
        Debug.DrawLine(rb.position, bottomLeftHit.point);
        Debug.DrawLine(rb.position, bottomRightHit.point);
        Debug.DrawLine(rb.position, bottomMiddleHit.point);

        bool newGrounded = false;
        Vector2 averageGroundNormal = (bottomLeftHit.normal + bottomRightHit.normal + bottomMiddleHit.normal).normalized;


        if (bottomLeftHit || bottomRightHit || bottomMiddleHit)
        {
            if (averageGroundNormal.y > stats.minGroundNormal)
            {
                newGrounded = true;
                groundNormal = averageGroundNormal;
            }
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

        if (((!topRightHit && topLeftHit) || (!topLeftHit && topRightHit)) && averageCeilNormal.y < stats.maxCeilNormal && topMiddleHit)
        {
            if (topLeftHit) ceilingNudgeDir = (topLeftHit.point - rb.position).normalized;
            if (topRightHit) ceilingNudgeDir = (topRightHit.point - rb.position).normalized;

            rb.position += ceilingNudgeDir * frameInput.Move;
        }
        else if (topLeftHit || topMiddleHit || topRightHit)
        {
            verticalVelocity.y = Mathf.Min(averageCeilNormal.y * stats.ceilingHitPush, verticalVelocity.y);
            shouldApplyGravityFallof = true;
        }
    }

    private void HandleWall(RaycastHit2D topHit, RaycastHit2D middleHit, RaycastHit2D bottomHit, RaycastHit2D topLeftHit, RaycastHit2D middlLeftHit, RaycastHit2D bottomLeftHit)
    {

        Vector2 averageWallNormal = Vector2.zero;
        bool newIsOnWall = false;

        if (middleHit && topHit)
        {
            averageWallNormal = (topHit.normal + middleHit.normal).normalized;
            newIsOnWall = Mathf.Abs(averageWallNormal.y) > 0 && Mathf.Abs(averageWallNormal.y) < stats.wallNormalRange && !isGrounded && leftSurfaceTime - time < stats.timeToWallGrab;

            if (newIsOnWall && frameInput.Move == 1) frameInput.Move = 0;
            wallNormal = averageWallNormal;
        }
        else if(topLeftHit && middlLeftHit)
        {
            averageWallNormal = (topLeftHit.normal + middlLeftHit.normal).normalized;
            newIsOnWall = Mathf.Abs(averageWallNormal.y) > 0 && Mathf.Abs(averageWallNormal.y) < stats.wallNormalRange && !isGrounded && leftSurfaceTime - time < stats.timeToWallGrab; ;

            if (newIsOnWall && frameInput.Move == -1) frameInput.Move = 0;
            wallNormal = averageWallNormal;
        }

        if (newIsOnWall ^ isOnWall) OnChangeWall(newIsOnWall);

        if (((bottomHit && !topHit && !middleHit) || (bottomLeftHit && !topLeftHit && !middlLeftHit)) && Mathf.Abs(averageWallNormal.y) > 0 && Mathf.Abs(averageWallNormal.y) < stats.wallNormalRange && !isGrounded)
        {
            int dir = bottomHit ? 1 : -1;
            Vector2 ledgeNormal = bottomHit? bottomHit.normal : bottomLeftHit.normal;

            float dist = Mathf.Abs(dir * bottomHit.point.x - dir * (rb.position.x + dir * PlayerHalfWidth));

            if (dist < stats.ledgeGrabDistance && ledgeNormal.y < stats.wallNormalRange)
            {
                if (verticalVelocity.y < 0) verticalVelocity.y = 0;
                Vector2 ledgeGrabDir = (rb.position - bottomHit.point).normalized;
                ledgeGrabDir.x *= -dir;

                rb.position += ledgeGrabDir;
            }
        }
    }

    #endregion




    #region Wall

    private bool isOnWall;
    private float wallFriction;
    private Vector2 wallNormal;

    private void OnChangeWall(bool newWall)
    {
        isOnWall = newWall;
        lastSurfaceType = LastSurfaceType.Wall;

        if (isOnWall)
        {
            isJumping = false;
            coyoteUsable = true;
            bufferedJumpUsable = true;
            shouldApplyGravityFallof = false;
            isGrounded = false;
            wallFriction = stats.onWallGrvaity;

            verticalVelocity = Vector2.zero;
        }
        else
        {
            leftSurfaceTime = time;
            if (!isJumping) shouldApplyGravityFallof = true;
        }
    }

    #endregion



    #region Ground
    private void OnChangeGrounded(bool newGrounded)
    {
        isGrounded = newGrounded;
        lastSurfaceType = LastSurfaceType.Ground;

        if (isGrounded)
        {
            isJumping = false;
            coyoteUsable = true;
            bufferedJumpUsable = true;
            shouldApplyGravityFallof = false;
        }
        else
        {
            leftSurfaceTime = time;
            if (!isJumping) shouldApplyGravityFallof = true;
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

        targetSpeed = Mathf.Lerp(stats.minLandSpeed, stats.maxLandSpeed, momentum) * moveInput;

        if (!isGrounded && !isOnWall)
        {
            targetSpeed *= stats.airSpeedMultiplier;
            targetSpeed += apexBonus;
        }
        else targetSpeed -= apexBonus;

        if (Mathf.Abs(frameInput.Move) > 0) acceleration = Mathf.Lerp(stats.minAcceleration, stats.maxAcceleration, momentum);
        else Mathf.Lerp(stats.minDeceleration, stats.maxDeceleration, momentum);

        if (!isGrounded && !isOnWall) acceleration *= stats.airAccelerationMultiplier;

        verticalVelocity.x = Mathf.MoveTowards(verticalVelocity.x, 0, acceleration * fixedDeltaTime);

        float speed = Mathf.MoveTowards(movementVelocity.magnitude * moveInput, targetSpeed, acceleration * fixedDeltaTime);
        movementVelocity = speed * groundHorizontal ;
    }

    #endregion



    #region Jump

    private enum LastSurfaceType
    {
        Wall,
        Ground
    }

    private LastSurfaceType lastSurfaceType = LastSurfaceType.Ground;

    private bool jumpRequested;
    private bool shouldApplyGravityFallof;
    private bool coyoteUsable;
    private bool bufferedJumpUsable;
    private float jumpPressedTime;
    private float leftSurfaceTime;
    private bool isJumping;


    public float ApexPoint
    {
        get
        {
            if (isJumping) return Mathf.InverseLerp(stats.jumpApexRange, 0, Mathf.Abs(rb.linearVelocityY));
            else return 0;
        }
    }

    private bool HasBufferedJump => bufferedJumpUsable && time < jumpPressedTime + stats.jumpBuffer;
    private bool CanUseCoyote => coyoteUsable && !isGrounded && time < leftSurfaceTime + stats.coyoteTime;

    private void HandleJump()
    {
        if (!isGrounded && !frameInput.JumpHeld && rb.linearVelocityY > 0 && isJumping) shouldApplyGravityFallof = true;

        if (!jumpRequested && !HasBufferedJump) return;

        if ((isGrounded || isOnWall || CanUseCoyote || HasBufferedJump) && jumpRequested) ExecuteJump();

        jumpRequested = false;
    }

    private void ExecuteJump()
    {
        isJumping = true;
        jumpRequested = false;
        shouldApplyGravityFallof = false;
        bufferedJumpUsable = false;
        coyoteUsable = false;


        if (lastSurfaceType == LastSurfaceType.Wall) 
        {
            verticalVelocity = stats.wallJumpForce * Vector2.LerpUnclamped(wallNormal, Vector2.up, stats.wallJumpUpBias);
            momentum += stats.wallJumpMomentumIncrease;
        }
        if (lastSurfaceType == LastSurfaceType.Ground) 
        {
            verticalVelocity = stats.jumpForce * Vector2.LerpUnclamped(groundNormal, Vector2.up, stats.jumpUpBias);
            momentum += stats.jumpMomentumIncrease;
        } 
    }

    #endregion




    #region Gravity

    private void HandleGravity()
    {
        if (isGrounded && !isJumping) verticalVelocity = Vector2.zero;
        if (!isGrounded)
        {
            float apexAntiGravity = Mathf.Lerp(0, -stats.apexAntigravity, ApexPoint);
            float downwardAcceleration;

            if (isOnWall && !isJumping) downwardAcceleration = stats.onWallGrvaity;
            else
            {
                downwardAcceleration = stats.gravity;
                downwardAcceleration += apexAntiGravity;

                if ((shouldApplyGravityFallof && verticalVelocity.y > 0 || verticalVelocity.y < stats.jumpVelocityFallof) && isJumping)
                {
                    downwardAcceleration *= stats.gravAfterFalloffMultiplier;
                }
            }

            verticalVelocity.y = Mathf.MoveTowards(verticalVelocity.y, -stats.maxDownVelocity, downwardAcceleration * Time.fixedDeltaTime);
        }
    }

    #endregion

    private void ApplyMovement()
    {
        rb.linearVelocity = movementVelocity + verticalVelocity;
    }
}
