using System;
using UnityEngine;

public class LandMovement : BaseMovementState
{
    public LandMovement(Player player, ScriptableObject movementStats, PlayerControls controls, Rigidbody2D rb, Collider2D col) : base(player, movementStats, controls, rb, col)
    {
        this.player = player;
        this.controls = controls;
        this.col = col;

        stats = (PlayerLandControllerStats)movementStats;
        playerRB = rb;

        momentum = stats.startingMomentum;
    }

    //TODO: change to private and refactor animator
    public struct Input
    {
        public bool DashDown;
        public bool JumpPressed;
        public bool JumpHeld;
        public float Move;
    }

    public Input frameInput { get; private set; }
    private float HorizontalInput => frameInput.Move;

    private readonly PlayerLandControllerStats stats;
    private float fixedDeltaTime;
    private float time;
    public override void Update()
    {
        time += Time.deltaTime;
        HandleInput();
    }

    protected override void HandleInput()
    {
        frameInput = new Input
        {
            JumpPressed = controls.PlayerLand.Jump.WasPressedThisFrame(),
            JumpHeld = controls.PlayerLand.Jump.IsPressed(),
            Move = controls.PlayerLand.HorizontalMove.ReadValue<float>(),
            DashDown = controls.PlayerLand.Dash.WasPressedThisFrame()
        };

        if (HorizontalInput == 1) isFacingRight = true;
        if (HorizontalInput == -1) isFacingRight = false;

        //Only changes dash requested if DashDown
        dashRequested = frameInput.DashDown || dashRequested;

        if (frameInput.JumpPressed)
        {
            jumpRequested = true;
            timeJumpRequested = time;
        }
    }

    public override void UpdateMovement()
    {
        fixedDeltaTime = Time.fixedDeltaTime;

        position = playerRB.position;
        vel = playerRB.linearVelocity;

        HandleCollisionInteractions();
        HandleJump();
        HandleGravity();
        HandleMomentum();
        HandleHorizontalMovement();
        HandleDash();
        HandleDrag();

        ApplyMovement();
    }

    private Vector2 position;
    public Vector2 vel;

    private Vector2 nonZeroVelDir;

    private float wallJumpHorizontalVel;
    private float horizontalVel;
    private float verticalVel;

    public event Action Jumped;
    public event Action WallJumped;
    public event Action<float> Moved;

    private bool isFacingRight;
    private int FacingDirection => isFacingRight ? 1 : -1;

    //Between 0 and 1, how strongly the desired input opposes with the  velocity
    private float VelocityOpposingMoveStrength(Vector2 vel) => Mathf.Clamp01(1 - Mathf.Abs(vel.normalized.x + FacingDirection));


    #region External Effectors
    private float SlipMult => wasOnSlipperyGround ? 1 / stats.slipStrength : 1;

    private bool isBeingSlowed;
    private Vector2 SlowAreaDrag
    {
        get
        {
            Vector2 drag = isBeingSlowed ? stats.slowAreaDrag : Vector2.zero;
            drag.y *= Mathf.Sign(verticalVel) == 1 ? 0.5f : 1; //Greater while falling, lower when rising so player can escape slow areas by jumping
            return drag;
        }
    }

    #endregion

    #region Terrain Collisions

    private float PlayerHalfWidth => col.bounds.extents.x;
    private float PlayerHalfHeight => col.bounds.extents.y;
    private TraversableTerrain terrainOn;

    /// <summary>
    /// Returns true if hit transform has component TraversableTerrain and changes terrain refrence accordingly
    /// </summary>
    private bool TryGetHitTerrain(RaycastHit2D hit, out TraversableTerrain terrain)
    {
        terrain = null;
        return hit ? hit.transform.TryGetComponent(out terrain) : false;
    }

    private void OnChangeTerrain(TraversableTerrain newTerrain)
    {
        terrainOn = newTerrain;
        if (terrainOn == null) return;

        wasOnSlipperyGround = terrainOn is SlipperyGround;
        if (terrainOn is not DisappearingSandPlatform) EventsManager.Instance.InvokePlayerLandOnStableGround();
        terrainOn.OnPlayerEnterTerrain(this);
    }

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
        Vector2 ceilingNudgeDir = Vector2.zero;
        Vector2 averageCeilNormal = (topLeftHit.normal + topMiddleHit.normal + topRightHit.normal).normalized;

        //CeilingNudge
        if(topLeftHit || topMiddleHit || topRightHit)
        {
            if (((!topRightHit && topLeftHit) || (!topLeftHit && topRightHit)) && averageCeilNormal.y < stats.maxCeilNormal && topMiddleHit)
            {
                if (topLeftHit) ceilingNudgeDir = (topLeftHit.point - position).normalized;
                if (topRightHit) ceilingNudgeDir = (topRightHit.point - position).normalized;

                position += ceilingNudgeDir * frameInput.Move;
            }
        }

        //Ceiling hit
        else
        {
            verticalVel = Mathf.Min(averageCeilNormal.y * stats.ceilingHitPush, verticalVel);
            shouldApplyGravFallof = true;
        }
    }

    #endregion

    #region Wall
    private bool isOnWall;
    public bool IsOnWall => isOnWall;
    private Vector2 wallNormal;

    private bool NormalInWallRange(Vector2 normal) => Mathf.Abs(normal.y) >= 0 && Mathf.Abs(normal.y) <= stats.wallNormalRange;
    private bool HitWall(RaycastHit2D hit) => hit && hit.transform.GetComponent<WallGrabbableTerrain>() != null;

    private void HandleWallDetection(RaycastHit2D topRightHit, RaycastHit2D middleRightHit, RaycastHit2D bottomRightHit, RaycastHit2D topLeftHit, RaycastHit2D middleLeftHit, RaycastHit2D bottomLeftHit)
    {
        TraversableTerrain newTerrain = null;
        Vector2 newWallNormal;
        bool newIsOnWall = false;
        bool canGrabWall = !isGrounded && vel.y < stats.maxYVelForWallGrab;

        HandleWallGrab(topRightHit, middleRightHit);
        HandleWallGrab(topLeftHit, middleLeftHit);

        void HandleWallGrab(RaycastHit2D topHit, RaycastHit2D middleHit)
        {
            RaycastHit2D hit = new();
            if (middleHit) hit = middleHit;
            else if (topHit) hit = topHit;

            newWallNormal = hit.normal;

            if (hit && NormalInWallRange(newWallNormal))
            {
                ResetAllHorizontalVel();

                if (HitWall(hit) && canGrabWall) 
                {
                    newIsOnWall = true;

                    TryGetHitTerrain(hit, out newTerrain);
                    lastSurfaceType = LastSurfaceType.Wall;

                    wallNormal = newWallNormal;
                }
            }
        }
        if (newIsOnWall ^ isOnWall) OnChangeWall(newIsOnWall);
        if (newTerrain ^ terrainOn) OnChangeTerrain(newTerrain);

        HandleLedgeGrab();
        void HandleLedgeGrab()
        {
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
                        verticalVel = Mathf.Max(0, verticalVel);

                        Vector2 ledgeGrabDir = (position - bottomRightHit.point).normalized;
                        ledgeGrabDir.x *= -dir;
                        ledgeGrabDir.y *= dir;

                        position += ledgeGrabDir;
                    }
                }
            }
        }

        //Only changes dashPrevented if on wall
        dashPrevented = newIsOnWall || dashPrevented;
    }


    private void OnChangeWall(bool newWall)
    {
        isOnWall = newWall;

        if (isOnWall)
        {
            isJumping = false;
            coyoteUsable = true;
            bufferedJumpUsable = true;
            shouldApplyGravFallof = false;
            isGrounded = false;
            dashPrevented = true;
            wasOnSlipperyGround = false;
        }
        else
        {
            leftSurfaceTime = time;
            if (!isJumping) shouldApplyGravFallof = true;
        }
    }

    #endregion

    #region Ground
    private bool isGrounded;
    private bool wasOnSlipperyGround;

    public bool IsGrounded => isGrounded;

    private void HandleGround(RaycastHit2D bottomLeftHit, RaycastHit2D bottomMiddleHit, RaycastHit2D bottomRightHit)
    {
        TraversableTerrain newTerrain = null;

        bool newGrounded = false;
        RaycastHit2D hit = new();

        //Priority is middle then right then left
        if (bottomMiddleHit) hit = bottomMiddleHit;
        else if (bottomRightHit) hit = bottomRightHit;
        else if (bottomLeftHit) hit = bottomLeftHit;

        Vector2 groundNormal = hit.normal;

        if (hit)
        {
            if (groundNormal.y > stats.minGroundNormal && TryGetHitTerrain(hit, out newTerrain))
            {
                newGrounded = true;
                lastSurfaceType = LastSurfaceType.Ground;
            }
        }
        if (isGrounded ^ newGrounded) OnChangeGrounded(newGrounded);
        if (newTerrain ^ terrainOn) OnChangeTerrain(newTerrain);

        dashPrevented = newGrounded || dashPrevented;
    }


    private void OnChangeGrounded(bool newGrounded)
    {
        isGrounded = newGrounded;

        if (isGrounded)
        {
            isJumping = false;
            coyoteUsable = true;
            bufferedJumpUsable = true;
            shouldApplyGravFallof = false;
        }
        else
        {
            leftSurfaceTime = time;

            //Applies gravity fallof if not jumping, such as walking off a ledge
            if (!isJumping) shouldApplyGravFallof = true;
        }
    }

    #endregion

    #region Momentum

    //Increases when performing actions, raising acceleration and max move speed
    public float momentum;

    private void HandleMomentum()
    {
        if ((Mathf.Abs(horizontalVel) > stats.momentumGainThreshold || Mathf.Abs(verticalVel) > stats.momentumGainThreshold))
        { momentum += stats.momentumGainSpeed * fixedDeltaTime; }
        else { momentum -= stats.momentumLossSpeed * fixedDeltaTime; }

        momentum = Mathf.Clamp01(momentum);
    }

    private void ApplyMomentum(float amount) => momentum = Mathf.Clamp01(momentum + amount);

    #endregion

    #region HorizontalMovement

    private float targetSpeed;
    private float horizontalAccel;

    private void HandleHorizontalMovement()
    {
        float moveInput = HorizontalInput;

        float turningMult = Mathf.Lerp(1, stats.turningAccelerationMultiplier * SlipMult, VelocityOpposingMoveStrength(vel));
        float apexBonus = moveInput * stats.apexSpeedIncrease * ApexProximity;

        //used for slower exit from wall
        float wallExitMult = lastSurfaceType == LastSurfaceType.Wall
            ? Mathf.Clamp01((time - leftSurfaceTime) / stats.timeToFullSpeedFromWall)
            : 1;

        horizontalAccel = Mathf.Abs(moveInput) > 0
            ? Mathf.Lerp(stats.minAcceleration, stats.maxAcceleration, momentum) * turningMult   //acceleration
            : Mathf.Lerp(stats.minDeceleration, stats.maxDeceleration, momentum);                //deceleration

        targetSpeed = moveInput * 1 / SlipMult * Mathf.Lerp(stats.minLandSpeed, stats.maxLandSpeed, momentum);

        if (IsInAir)
        {
            targetSpeed = targetSpeed * stats.airSpeedMultiplier + wallJumpHorizontalVel + apexBonus;
            horizontalAccel *= stats.airAccelerationMultiplier * horizontalVelMult;
        }
        else horizontalAccel *= SlipMult;   //For slower acceleration on ground when slippery

        float desiredSpeed = Mathf.MoveTowards(horizontalVel, targetSpeed, horizontalAccel * fixedDeltaTime);
        horizontalVel = desiredSpeed * wallExitMult + wallJumpHorizontalVel;

        wallJumpHorizontalVel = Mathf.MoveTowards(wallJumpHorizontalVel, 0, Mathf.Abs(horizontalVel) * stats.additionalHorizontalMovementDeceleration * fixedDeltaTime);
    }

    private void ResetAllHorizontalVel()
    {
        horizontalVel = 0;
        wallJumpHorizontalVel = 0;
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

    private bool shouldApplyGravFallof;
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
            if (isJumping) return Mathf.InverseLerp(stats.jumpApexRange, 0, Mathf.Abs(vel.y));
            else return 0;
        }
    }

    private bool HasBufferedJump => bufferedJumpUsable && time < timeJumpRequested + stats.jumpBuffer;
    private bool CanUseCoyote => coyoteUsable && !isGrounded && !isOnWall && time < leftSurfaceTime + stats.coyoteTime;

    private void HandleJump()
    {
        if (isDashing) return;

        //If jump ended early, apply gravity fallof
        if (!isGrounded && !frameInput.JumpHeld && verticalVel > 0 && isJumping) shouldApplyGravFallof = true;

        if (!jumpRequested && !HasBufferedJump) return;

        if (!IsInAir || CanUseCoyote) ExecuteJump();

        jumpRequested = false;
    }

    private void ExecuteJump()
    {
        isJumping = true;
        jumpRequested = false;
        shouldApplyGravFallof = false;
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

        Vector2 jumpVel = stats.wallJumpVel * Vector2.LerpUnclamped(wallNormal, Vector2.up, stats.wallJumpUpBias);

        verticalVel = jumpVel.y;
        wallJumpHorizontalVel = jumpVel.x;

        ApplyMomentum(stats.wallJumpMomentumIncrease);
    }
    private void GroundJump()
    {
        Jumped?.Invoke();

        verticalVel = stats.jumpVelocity;
        ApplyMomentum(stats.jumpMomentumIncrease);
    }

    #endregion

    #region Dash

    private bool dashRequested;
    private bool isDashing;

    private float horizontalVelMult;
    private float verticalVelMult;

    private float timeDashed = float.MinValue;
    private bool dashPrevented;
    private Vector2 dashVel;
    private Vector2 targetDashVel;

    private void HandleDash()
    {
        bool canDash = !isDashing && !dashPrevented;

        if (dashRequested && canDash) ExecuteDash();

        dashVel = Vector2.zero;
        horizontalVelMult = 1;
        verticalVelMult = 1;
        dashRequested = false;

        if (dashPrevented || !isDashing)
        {
            ResetDash();
            return;
        }

        float timePercent = (time - timeDashed) / stats.dashDuration;

        horizontalVelMult = stats.dashHorizontalVelMult.Evaluate(timePercent);
        verticalVelMult = stats.dashVerticalVelMult.Evaluate(timePercent);

        float speedPercent = stats.dashSpeedCurve.Evaluate(timePercent);
        dashVel = targetDashVel * speedPercent;

        //Applies friction if player is trying to oppose dash velocity for greater control
        dashVel.x = Mathf.MoveTowards(dashVel.x, 0, VelocityOpposingMoveStrength(dashVel) * fixedDeltaTime * stats.dashOpposingMovementFriction * Mathf.Abs(dashVel.x));
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

        shouldApplyGravFallof = true; //To prevent floaty dash if jump and dash are executed at the same time
        verticalVel = 0;

        targetDashVel = frameInput.JumpHeld ? stats.diagonalDashVel : stats.horizontalDashVel;
        targetDashVel.x *= FacingDirection;

        //Prevents horizontal velocity opposite dash velocity when doing sudden turn dashes
        horizontalVel = FacingDirection * Mathf.Max(horizontalVel * FacingDirection, 0);
    }

    #endregion

    #region Gravity

    public bool IsInAir => !isGrounded && !isOnWall;
    private float currentGravity;
    private void HandleGravity()
    {
        if (isGrounded) return;
        if (isOnWall) currentGravity = stats.onWallGravity;
        else
        {
            float apexAntiGravity = Mathf.Lerp(0, -stats.apexAntigravity, ApexProximity);
            float gravFallof = shouldApplyGravFallof && verticalVel > 0 || verticalVel < stats.jumpVelocityFallof
                ? stats.gravAfterFalloffMultiplier
                : 1;

            currentGravity = (stats.gravity + apexAntiGravity) * gravFallof;
        }

        verticalVel = Mathf.MoveTowards(verticalVel, -stats.maxDownVel, currentGravity * fixedDeltaTime * verticalVelMult);
    }

    #endregion

    private void HandleDrag()
    {
        Vector2 drag = SlowAreaDrag;

        horizontalVel = Mathf.MoveTowards(horizontalVel, 0, drag.x * fixedDeltaTime * Mathf.Abs(horizontalVel));
        verticalVel = Mathf.MoveTowards(verticalVel, 0, drag.y * fixedDeltaTime * Mathf.Abs(verticalVel));
        if (isBeingSlowed) dashPrevented = true;

        if (isGrounded) verticalVel = stats.groundingPush;
    }

    private void ApplyMovement()
    {
        playerRB.position = position;
        playerRB.linearVelocity = vel;

        vel = Vector2.up * verticalVel + Vector2.right * (horizontalVel * horizontalVelMult) + dashVel;

        nonZeroVelDir = new Vector2(
            vel.x != 0 ? vel.x : nonZeroVelDir.x,
            vel.y != 0 ? vel.y : nonZeroVelDir.y)
            .normalized;
    }


    #region Triggers
    public override void TriggerEnter(IPlayerCollisionInteractor collisionListener)
    {
        if (collisionListener == null) return;
        if (collisionListener is SlowingArea)
        {
            isBeingSlowed = true;
            dashPrevented = true;
            ApplyMomentum(-stats.slowAreaMomentumLoss);
        }
    }
    public override void TriggerExit(IPlayerCollisionInteractor collisionListener)
    {
        if (collisionListener == null) return;
        if (collisionListener is SlowingArea) isBeingSlowed = false;
    }
    #endregion
}
