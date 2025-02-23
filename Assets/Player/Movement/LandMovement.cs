using System;
using UnityEngine;

public class LandMovement : BaseMovementState
{
    private readonly PlayerLandControllerStats stats;

    public LandMovement(Player player, ScriptableObject movementStats, PlayerControls controls, Rigidbody2D rb, Collider2D col) : base(player, movementStats, controls, rb, col)
    {
        this.player = player;
        this.controls = controls;
        this.playerCol = col;

        stats = (PlayerLandControllerStats)movementStats;
        playerRB = rb;
    }

    public struct Input
    {
        public bool DashDown;
        public bool JumpDown;
        public bool JumpHeld;
        public float Move;
    }
    public Input frameInput { get; private set; }

    protected override void HandleInput()
    {
        frameInput = new Input
        {
            JumpDown = controls.PlayerLand.Jump.WasPressedThisFrame(),
            JumpHeld = controls.PlayerLand.Jump.IsPressed(),
            Move = controls.PlayerLand.Move.ReadValue<float>(),
            DashDown = controls.PlayerLand.Dash.WasPressedThisFrame()
        };

        if (frameInput.Move == 1) isFacingRight = true;
        if (frameInput.Move == -1) isFacingRight = false;

        //Only changes dash requested if DashDown
        dashRequested = frameInput.DashDown || dashRequested;

        if (frameInput.JumpDown)
        {
            jumpRequested = true;
            timeJumpRequested = time;
        }
    }

    public override void Update()
    {
        time += Time.deltaTime;
        HandleInput();
    }

    private float time;
    private float fixedDeltaTime;
    public override void UpdateMovement()
    {
        fixedDeltaTime = Time.fixedDeltaTime;

        position = playerRB.position;
        velocity = playerRB.linearVelocity;

        HandleCollisionInteractions();
        HandleJump();
        HandleGravity();
        HandleMomentum();
        HandleHorizontalMovement();
        HandleDash();

        ApplyMovement();
    }

    private Vector2 position;
    public Vector2 velocity { get; private set; }

    private Vector2 groundNormal = Vector2.up;
    private Vector2 groundHorizontal = Vector2.right;

    private Vector2 horizontalVelocity;
    private Vector2 jumpAndGravVelocity;

    public event Action Jumped;
    public event Action WallJumped;
    public event Action<float> Moved;

    private bool isFacingRight;
    private int FacingDirection => isFacingRight ? 1 : -1;
    private float SlipMultiplier => wasOnSlipperyGround ? 1 / stats.slipStrength : 1;

    #region Collisions
    private float PlayerHalfWidth => playerCol.bounds.extents.x;
    private float PlayerHalfHeight => playerCol.bounds.extents.y;

    private void HandleCollisionInteractions()
    {
        Vector2 rightOrigin = Vector2.right * (PlayerHalfWidth - stats.skinWidth);
        Vector2 downOrigin = Vector3.down * (PlayerHalfHeight - stats.skinWidth);
        HandleWallDetection(
        Physics2D.Raycast(position - downOrigin + rightOrigin, Vector2.right, stats.wallDetectionDistance, stats.collisionLayerMask),
        Physics2D.Raycast(position + rightOrigin, Vector2.right, stats.wallDetectionDistance, stats.collisionLayerMask),
        Physics2D.Raycast(position + downOrigin + rightOrigin, Vector2.right, stats.wallDetectionDistance, stats.collisionLayerMask),
        Physics2D.Raycast(position - downOrigin - rightOrigin, Vector2.left, stats.wallDetectionDistance, stats.collisionLayerMask),
        Physics2D.Raycast(position - rightOrigin, Vector2.left, stats.wallDetectionDistance, stats.collisionLayerMask),
        Physics2D.Raycast(position + downOrigin - rightOrigin, Vector2.left, stats.wallDetectionDistance, stats.collisionLayerMask)
        );
        HandleGround(
        Physics2D.Raycast(position + downOrigin - rightOrigin, Vector2.down, stats.groundedDistance, stats.collisionLayerMask),
        Physics2D.Raycast(position + downOrigin + rightOrigin, Vector2.down, stats.groundedDistance, stats.collisionLayerMask),
        Physics2D.Raycast(position + downOrigin, Vector2.down, stats.groundedDistance, stats.collisionLayerMask)
        );
        HandleCeiling(
        Physics2D.Raycast(position - downOrigin - rightOrigin, Vector2.up, stats.ceilingDistance, stats.collisionLayerMask),
        Physics2D.Raycast(position - downOrigin + rightOrigin, Vector2.up, stats.ceilingDistance, stats.collisionLayerMask),
        Physics2D.Raycast(position - downOrigin, Vector2.up, stats.ceilingDistance, stats.collisionLayerMask)
        );
    }

    private void HandleCeiling(RaycastHit2D topLeftHit, RaycastHit2D topMiddleHit, RaycastHit2D topRightHit)
    {
        Debug.DrawLine(position, topLeftHit.point);
        Debug.DrawLine(position, topRightHit.point);
        Debug.DrawLine(position, topMiddleHit.point);

        Vector2 ceilingNudgeDir = Vector2.zero;
        Vector2 averageCeilNormal = (topLeftHit.normal + topMiddleHit.normal + topRightHit.normal).normalized;

        if (((!topRightHit && topLeftHit) || (!topLeftHit && topRightHit)) && averageCeilNormal.y < stats.maxCeilNormal && topMiddleHit)
        {
            if (topLeftHit) ceilingNudgeDir = (topLeftHit.point - position).normalized;
            if (topRightHit) ceilingNudgeDir = (topRightHit.point - position).normalized;

            position += ceilingNudgeDir * frameInput.Move;
        }
        else if (topLeftHit || topMiddleHit || topRightHit)
        {
            jumpAndGravVelocity.y = Mathf.Min(averageCeilNormal.y * stats.ceilingHitPush, jumpAndGravVelocity.y);
            shouldApplyGravityFallof = true;
        }
    }

    #endregion

    #region Wall
    public bool isOnWall { get; private set; }
    private Vector2 wallNormal;

    private bool NormalInWallRange(Vector2 normal) => 
        Mathf.Abs(normal.y) >= 0 
        && Mathf.Abs(normal.y) <= stats.wallNormalRange;
    private bool HitWall(RaycastHit2D hit) => hit.transform.TryGetComponent(out Terrain terrain) && terrain.isWall;

    private void HandleWallDetection(RaycastHit2D topRightHit, RaycastHit2D middleRightHit, RaycastHit2D bottomRightHit, RaycastHit2D topLeftHit, RaycastHit2D middleLeftHit, RaycastHit2D bottomLeftHit)
    {
        Vector2 averageWallNormal;
        bool newIsOnWall = false;
        bool canGrabWall = !isGrounded  && velocity.y < stats.maxYVelocityForWallGrab;

        HandleWallGrab(topRightHit, middleRightHit);
        HandleWallGrab(topLeftHit, middleLeftHit);

        void HandleWallGrab(RaycastHit2D topHit, RaycastHit2D middleHit)
        {
            averageWallNormal = (topHit.normal + middleHit.normal).normalized;

            bool wallGrabbable = NormalInWallRange(averageWallNormal) 
                && ((middleHit && HitWall(middleHit)) || (topHit && HitWall(topHit)));

            if (wallGrabbable && canGrabWall)
            {
                newIsOnWall = true;
                lastSurfaceType = LastSurfaceType.Wall;

                horizontalVelocity.x = 0;
                jumpAndGravVelocity.x = 0;

                wallNormal = averageWallNormal;
            }
        }

        if (newIsOnWall ^ isOnWall) OnChangeWall(newIsOnWall);

        //Only changes dash interrupted if on wall
        dashPrevented = newIsOnWall || dashPrevented;

        //Ledge grab
        if (((bottomRightHit && !topRightHit && !middleRightHit) || (bottomLeftHit && !topLeftHit && !middleLeftHit)) && !isGrounded)
        {
            RaycastHit2D ledgeGrabHit = bottomRightHit ? bottomRightHit : bottomLeftHit;

            //Ledge grabs only on non-wall-grabbable walls for greater control
            if (!HitWall(ledgeGrabHit))
            {
                int dir = bottomRightHit ? 1 : -1;
                float dist = Mathf.Abs(ledgeGrabHit.point.x - (position.x + dir * PlayerHalfWidth));

                Vector2 ledgeNormal = ledgeGrabHit.normal;

                if (dist < stats.ledgeGrabDistance && NormalInWallRange(ledgeNormal))
                {
                    if (jumpAndGravVelocity.y < 0) jumpAndGravVelocity.y = 0;
                    Vector2 ledgeGrabDir = (position - bottomRightHit.point).normalized;
                    ledgeGrabDir.x *= -dir;
                    ledgeGrabDir.y *= dir;

                    position += ledgeGrabDir;
                }
            }
        }
    }
    private void OnChangeWall(bool newWall)
    {
        isOnWall = newWall;

        if (isOnWall)
        {
            isJumping = false;
            coyoteUsable = true;
            bufferedJumpUsable = true;
            shouldApplyGravityFallof = false;
            isGrounded = false;
            dashPrevented = true;
        }
        else
        {
            leftSurfaceTime = time;
            if (!isJumping) shouldApplyGravityFallof = true;
        }
    }

    #endregion

    #region Ground
    private bool isGrounded;
    private bool wasOnSlipperyGround;

    public bool IsGrounded => isGrounded;
    private bool HitIsSlippery(RaycastHit2D hit) => hit && hit.transform.TryGetComponent(out Terrain terrain) && terrain.isSlippery;

    private void HandleGround(RaycastHit2D bottomLeftHit, RaycastHit2D bottomMiddleHit, RaycastHit2D bottomRightHit)
    {
        bool newGrounded = false;
        Vector2 averageGroundNormal = (bottomLeftHit.normal + bottomRightHit.normal + bottomMiddleHit.normal).normalized;

        if (bottomLeftHit || bottomRightHit || bottomMiddleHit)
        {
            wasOnSlipperyGround = HitIsSlippery(bottomLeftHit) || HitIsSlippery(bottomMiddleHit) || HitIsSlippery(bottomRightHit);

            if (averageGroundNormal.y > stats.minGroundNormal)
            {
                newGrounded = true;
                groundNormal = averageGroundNormal;
                lastSurfaceType = LastSurfaceType.Ground;
            }
        }

        dashPrevented = newGrounded || dashPrevented;
        if (isGrounded ^ newGrounded) OnChangeGrounded(newGrounded);
    }


    private void OnChangeGrounded(bool newGrounded)
    {
        isGrounded = newGrounded;

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

            //Applies gravity fallof if not jumping, such as walking off a ledge
            if (!isJumping) shouldApplyGravityFallof = true;
        }
    }

    #endregion

    #region Momentum

    //Increases when performing actions, raising acceleration and max move speed
    public float momentum;

    private void HandleMomentum()
    {
        if ((horizontalVelocity.magnitude > stats.momentumGainThreshold || jumpAndGravVelocity.magnitude > stats.momentumGainThreshold))
        { momentum += stats.momentumGainSpeed * fixedDeltaTime; }
        else { momentum -= stats.momentumLossSpeed * fixedDeltaTime; }

        momentum = Mathf.Clamp01(momentum);
    }

    private void ApplyMomentum(float amount) => momentum = Mathf.Clamp01(momentum + amount);

    #endregion

    #region HorizontalMovement

    private float targetSpeed;
    private float acceleration;

    //How strongly the desired input opposes with the current velocity
    public float OpposingMovementStrength => 1 - (playerRB.linearVelocity.normalized.x + frameInput.Move);

    private void HandleHorizontalMovement()
    {
        float moveInput = frameInput.Move;
        float absMoveInput = Mathf.Abs(moveInput);

        float turningMult = Mathf.Lerp(1, stats.turningAccelerationMultiplier 
            //*  SlipMultiplier
            , OpposingMovementStrength);
        float apexBonus = moveInput * stats.apexSpeedIncrease * ApexProximity;

        //used for slower exit from wall
        float wallExitMult = lastSurfaceType == LastSurfaceType.Wall 
            ? Mathf.Clamp01( (time - leftSurfaceTime) / stats.timeToFullSpeedFromWall )
            : 1;

        acceleration = absMoveInput > 0 
            ? Mathf.Lerp(stats.minAcceleration, stats.maxAcceleration, momentum) * turningMult   //acceleration
            : Mathf.Lerp(stats.minDeceleration, stats.maxDeceleration, momentum);                //deceleration

        targetSpeed = moveInput * 1 / SlipMultiplier * Mathf.Lerp(stats.minLandSpeed, stats.maxLandSpeed, momentum);

        if (IsInAir)
        {
            targetSpeed = targetSpeed * stats.airSpeedMultiplier + apexBonus;
            acceleration *= stats.airAccelerationMultiplier;
        }
        else acceleration *= SlipMultiplier;   //For faster aceleration in air when slippery

        float desiredSpeed = Mathf.MoveTowards(horizontalVelocity.x, targetSpeed, acceleration * fixedDeltaTime);
        float speed = desiredSpeed * wallExitMult;

        horizontalVelocity = speed * dashSpeedMult * groundHorizontal;
        Debug.Log(horizontalVelocity);

        //Decelerates horizontal velocity from jump by movement acceleration for greater control
        jumpAndGravVelocity.x = Mathf.MoveTowards(jumpAndGravVelocity.x, 0, acceleration * fixedDeltaTime);
    }

    #endregion

    #region Jump

    private enum LastSurfaceType
    {
        Wall,
        Ground
    }

    //Last surface type is cahed to allow coyote jumping from both walls and ground
    private LastSurfaceType lastSurfaceType = LastSurfaceType.Ground;

    private bool shouldApplyGravityFallof;
    private bool coyoteUsable;
    private bool bufferedJumpUsable;
    private float leftSurfaceTime = float.MinValue;
    private bool isJumping;
    private bool jumpRequested;
    private float timeJumpRequested = float.MinValue;

    public float ApexProximity
    {
        get
        {
            if (isJumping) return Mathf.InverseLerp(stats.jumpApexRange, 0, Mathf.Abs(velocity.y));
            else return 0;
        }
    }

    private bool HasBufferedJump => bufferedJumpUsable && time < timeJumpRequested + stats.jumpBuffer;
    private bool CanUseCoyote => coyoteUsable && !isGrounded && !isOnWall && time < leftSurfaceTime + stats.coyoteTime;

    private void HandleJump()
    {
        if (isDashing) return;

        //If jump ended early, apply gravity fallof
        if (!isGrounded && !frameInput.JumpHeld && velocity.y > 0 && isJumping) shouldApplyGravityFallof = true;

        if (!jumpRequested && !HasBufferedJump) return;

        if (isGrounded || isOnWall || CanUseCoyote) ExecuteJump();

        jumpRequested = false;
    }

    private void ExecuteJump()
    {
        isJumping = true;
        jumpRequested = false;
        shouldApplyGravityFallof = false;
        bufferedJumpUsable = false;
        coyoteUsable = false;
        timeJumpRequested = 0;

        switch (lastSurfaceType)
        {
            case LastSurfaceType.Wall:
                WallJump();
                break;

            case LastSurfaceType.Ground:
                GroundJump();
                break;
        }
    }

    private void WallJump()
    {
        WallJumped?.Invoke();

        jumpAndGravVelocity = stats.wallJumpVelocity * Vector2.LerpUnclamped(wallNormal, Vector2.up, stats.wallJumpUpBias);
        ApplyMomentum(stats.wallJumpMomentumIncrease);
    }
    private void GroundJump()
    {
        Jumped?.Invoke();

        jumpAndGravVelocity = stats.jumpVelocity * Vector2.LerpUnclamped(groundNormal, Vector2.up, stats.jumpUpBias);
        ApplyMomentum(stats.jumpMomentumIncrease);
    }

    #endregion

    #region Gravity

    public bool IsInAir => !isGrounded && !isOnWall;
    private void HandleGravity()
    {
        if (isGrounded) return;

        float downwardAcceleration;

        if (isOnWall && !isJumping) downwardAcceleration = stats.onWallGravity;
        else
        {
            float apexAntiGravity = Mathf.Lerp(0, -stats.apexAntigravity, ApexProximity);
            float gravFallof = shouldApplyGravityFallof && jumpAndGravVelocity.y > 0 || jumpAndGravVelocity.y < stats.jumpVelocityFallof
                ? stats.gravAfterFalloffMultiplier
                : 1;

            downwardAcceleration = (stats.gravity + apexAntiGravity) * gravFallof;
        }

        jumpAndGravVelocity.y = Mathf.MoveTowards(jumpAndGravVelocity.y, -stats.maxDownVelocity, downwardAcceleration * fixedDeltaTime * dashGravMult);
    }

    #endregion

    #region Dash

    private bool dashRequested;
    private bool isDashing;

    private float dashSpeedMult;
    private float dashGravMult;

    private float timeDashed = float.MinValue;
    private bool dashPrevented;
    private Vector2 dashVelocity;
    private Vector2 targetDashVelocity;

    private void HandleDash()
    {
        bool canDash = !isDashing && !dashPrevented;

        if (dashRequested && canDash) ExecuteDash();

        dashVelocity = Vector2.zero;
        dashSpeedMult = 1;
        dashGravMult = 1;
        dashRequested = false;

        if (dashPrevented || !isDashing)
        {
            ResetDash();
            return; 
        }

        float timePercent = (time - timeDashed) / stats.dashDuration;

        dashSpeedMult = stats.dashHorizontalCurve.Evaluate(timePercent);
        dashGravMult = stats.dashVerticalCurve.Evaluate(timePercent);

        float speedPercent = stats.dashSpeedCurve.Evaluate(timePercent);
        dashVelocity = targetDashVelocity * speedPercent;
    }

    private void ResetDash()
    {
        isDashing = false;
        dashPrevented = false;
    }

    private void ExecuteDash()
    {
        isDashing = true; 
        timeDashed = time;
        dashPrevented = false;

        //To prevent floaty dash if jump and dash are executed at the same time
        shouldApplyGravityFallof = true;

        float verticalMult = frameInput.JumpHeld ? stats.dashInputUpMultiplier: 1;
        targetDashVelocity = new Vector2(stats.targetDashVelocity.x * FacingDirection, stats.targetDashVelocity.y * verticalMult);

        //Keeps upwards velocity but nullifies it if falling to prevent downwards dashes
        jumpAndGravVelocity.y = 0;

        //Prevents horizontal velocity opposite dash velocity when doing sudden turn dashes
        horizontalVelocity.x = FacingDirection * Mathf.Max(horizontalVelocity.x * FacingDirection, 0);
    }

    #endregion

    private void ApplyMovement()
    {
        if (isGrounded && !isJumping && !isDashing) jumpAndGravVelocity.y = stats.groundingPush;
        velocity = jumpAndGravVelocity + horizontalVelocity + dashVelocity;

        playerRB.position = position;
        playerRB.linearVelocity = velocity;
    }
}
