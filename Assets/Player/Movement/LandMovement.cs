using System;
using UnityEngine;
using static TransitionLibrary;

public class LandMovement : IState
{
    private readonly MovementStatsHolder sharedStats;
    private readonly LandMovementStats stats;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;

    private float PlayerHalfWidth => col.bounds.extents.x;
    private float PlayerHalfHeight => col.bounds.extents.y;

    public LandMovement(Rigidbody2D rb, Collider2D col, MovementStatsHolder stats)
    {
        this.col = col;
        sharedStats = stats;
        this.stats = stats.landStats;
        this.rb = rb;
    }

    public void InitializeTransitions(MovementStateMachine controller)
    {
        controller.AddTransition(GetType(), typeof(SandEntryMovement), TransitionToSandEntryDash);
        controller.AddTransition(GetType(), typeof(SandEntryMovement), TransitionToDirectSandEntryDash);
        controller.AddTransition(GetType(), typeof(BurrowMovement), TransitionToSandEntry);

    }

    public void EnterState(IStateSpecificTransitionData lastStateData)
    {
        if (lastStateData is LandMovementTransition transitionData)
        {
            col.transform.rotation = Quaternion.identity;

            invalidSandDashSand = transitionData.EntrySand;
            float launchSpeed = transitionData.EnteredWithDash
                ? invalidSandDashSand.LaunchSpeed
                : invalidSandDashSand.WeakLaunchSpeed;

            ExecuteEntryLaunch(launchSpeed * transitionData.EntryDir);
        }
    }

    public void ExitState()
    {
        time = 0;
        ResetAllVelocities();
        ResetEntryLaunch();
        ResetDash();
        ResetTerrain();
        ResetJump();
        ResetSandEntryDash();
    }

    private Player.Input frameInput;
    private float HorizontalInput => frameInput.HorizontalMove;
    private float fixedDeltaTime;
    private float time;

    public void Update(Player.Input frameInput)
    {
        time += Time.deltaTime;
        HandleInput(frameInput);
        UpdateSandDash();
    }
    public void HandleInput(Player.Input frameInput)
    {
        this.frameInput = frameInput;

        if (HorizontalInput == 1) isFacingRight = true;
        if (HorizontalInput == -1) isFacingRight = false;

        //Only changes dash requested if DashDown
        if (frameInput.DashDown) dashRequested = true;
        if (frameInput.SandDashDown)
        {
            sandDashRequested = true;
            timeSandDashRequested = time; 
        }

        if (frameInput.JumpPressed)
        {
            jumpRequested = true;
            timeJumpRequested = time;
        }
    }


    public void UpdateMovement()
    {
        fixedDeltaTime = Time.fixedDeltaTime;
        position = rb.position;

        HandleCollisionInteractions();
        HandleJump();
        HandleGravity();
        HandleDash();
        HandleEntryLaunch();
        HandleHorizontalMovement();
        HandleMomentum();
        HandleVelocity();
        ApplyMovement();
    }

    private Vector2 position;
    public Vector2 vel;

    private Vector2 nonZeroVelocityDirection;

    private float wallJumpHorizontalVel;
    private float horizontalVel;
    private float verticalVel;

    public event Action Jumped;
    public event Action WallJumped;
    public event Action<float> Moved;

    private Vector2 FrameDisplacement => vel * fixedDeltaTime;
    /// <summary>
    /// Between 0 and 1, how strongly the desired input opposes with the current velocity
    /// </summary>
    private float VelocityOpposingMovementStrength(Vector2 vel) => Mathf.Clamp01(1 - Mathf.Abs(vel.normalized.x + FacingDirection));

    #region ExternalEffectors
    private bool isFacingRight;
    private int FacingDirection => isFacingRight ? 1 : -1;
    private float SlipMultiplier => wasOnSlipperyGround ? 1 / stats.slipStrength : 1;

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

    #region EntryLaunch
    private bool isEntryLaunching;
    private bool entryLaunchInterrupted;
    private float entryLaunchProgress;

    private Vector2 entryLaunchVel;
    private Vector2 wishLaunchVel;
    private Vector2 initialLaunchVel;

    private float entryLaunchGravMult;
    private float entryLaunchControlMult;

    private void HandleEntryLaunch()
    {
        entryLaunchProgress = Mathf.Clamp01(time / stats.launchDuration);

        if (isBeingSlowed
         || (entryLaunchProgress == 1 && isEntryLaunching)
         || isDashing || hitCeilingThisFrame || isPushingWall)
            entryLaunchInterrupted = true;

        if (entryLaunchInterrupted) ExitEntryLaunch();
        else if (isEntryLaunching)
        {
            entryLaunchGravMult = stats.launchGravCurve.Evaluate(entryLaunchProgress);
            entryLaunchControlMult = stats.launchControlCurve.Evaluate(entryLaunchProgress);

            entryLaunchVel.x = wishLaunchVel.x * stats.launchSpeedCurve.Evaluate(entryLaunchProgress);

            float sign = Mathf.Sign(wishLaunchVel.x);
            float oppositeMovementReduction = Mathf.Approximately(sign, -Mathf.Sign(horizontalDelta)) && horizontalDelta != 0
                ? Mathf.Abs(horizontalDelta) : 0;
            wishLaunchVel.x = Mathf.MoveTowards(wishLaunchVel.x, 0, stats.launchOpposingMovementFriction * oppositeMovementReduction);
        }
    }

    private void ExecuteEntryLaunch(Vector2 launchVel)
    {
        wishLaunchVel = launchVel;

        initialLaunchVel = wishLaunchVel;

        verticalVel = 0;
        entryLaunchVel.y = wishLaunchVel.y;
        shouldApplyGravFallof = true;
        isEntryLaunching = true;
    }
    private void ResetEntryLaunch()
    {
        entryLaunchInterrupted = false;
        isEntryLaunching = false;
        entryLaunchVel = Vector2.zero;
        entryLaunchGravMult = 1;
        entryLaunchControlMult = 1;
    }

private void ExitEntryLaunch()
    {
        entryLaunchInterrupted = false;
        isEntryLaunching = false;
        entryLaunchVel = Vector2.zero;
        entryLaunchGravMult = 1;
        entryLaunchControlMult = 1;
    }

    #endregion

    #region TerrainBehaviour

    public enum TerrainInteractionType
    {
        Ground,
        Ceiling,
        Wall
    }

    TraversableTerrain terrainOn;
    private void HandleTerrainChange(TraversableTerrain newTerrain, TerrainInteractionType interactionType)
    {
        if (newTerrain == null || newTerrain == terrainOn) return;
        terrainOn = newTerrain;

        wasOnSlipperyGround = newTerrain is SlipperyGround;

        if (interactionType == TerrainInteractionType.Wall)
            EventsHolder.PlayerEvents.InvokePlayerGrabWall(newTerrain);
        if (interactionType == TerrainInteractionType.Ground)
            EventsHolder.PlayerEvents.InvokePlayerLandOnGround(newTerrain);

        newTerrain.OnEnterTerrain();
    }

    private void HandleTerrainTouch(TraversableTerrain terrainTouched, TerrainInteractionType interactionType)
    {
        if (terrainTouched == null || terrainTouched == terrainOn) return;
        terrainTouched.OnCollideWithTerrain(interactionType);
    }

    /// <summary>
    /// Returns true if hit transform has component TraversableTerrain and changes terrain refrence accordingly
    /// </summary>
    private bool TryGetHitTerrain(RaycastHit2D hit, out TraversableTerrain terrain)
    {
        terrain = null;
        return hit ? hit.transform.TryGetComponent(out terrain) : false;
    }

    private void HandleCollisionInteractions()
    {
        Vector2 rightOrigin = Vector2.right * (PlayerHalfWidth - stats.skinWidth);
        Vector2 downOrigin = Vector2.down * (PlayerHalfHeight - stats.skinWidth);
        HandleWallDetection(
        Physics2D.Raycast(position - downOrigin + rightOrigin, Vector2.right, stats.wallDetectionDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(position + rightOrigin, Vector2.right, stats.wallDetectionDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(position + downOrigin + rightOrigin, Vector2.right, stats.wallDetectionDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(position - downOrigin - rightOrigin, Vector2.left, stats.wallDetectionDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(position - rightOrigin, Vector2.left, stats.wallDetectionDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(position + downOrigin - rightOrigin, Vector2.left, stats.wallDetectionDistance, sharedStats.collisionLayerMask)
        );
        HandleGround(
        Physics2D.Raycast(position + downOrigin - rightOrigin, Vector2.down, stats.groundedDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(position + downOrigin + rightOrigin, Vector2.down, stats.groundedDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(position + downOrigin, Vector2.down, stats.groundedDistance, sharedStats.collisionLayerMask)
        );
        HandleCeiling(
        Physics2D.Raycast(position - downOrigin - rightOrigin, Vector2.up, stats.ceilingDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(position - downOrigin + rightOrigin, Vector2.up, stats.ceilingDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(position - downOrigin, Vector2.up, stats.ceilingDistance, sharedStats.collisionLayerMask)
        );
    }

    private bool hitCeilingThisFrame;
    private void HandleCeiling(RaycastHit2D topLeftHit, RaycastHit2D topMiddleHit, RaycastHit2D topRightHit)
    {
        hitCeilingThisFrame = false;
        Vector2 ceilingNudgeDir = Vector2.zero;
        Vector2 averageCeilNormal = (topLeftHit.normal + topMiddleHit.normal + topRightHit.normal).normalized;

        if (((!topRightHit && topLeftHit) || (!topLeftHit && topRightHit)) && topMiddleHit)
        {
            if (topLeftHit) ceilingNudgeDir = (topLeftHit.point - position).normalized;
            if (topRightHit) ceilingNudgeDir = (topRightHit.point - position).normalized;
            position += ceilingNudgeDir * Mathf.Sign(nonZeroVelocityDirection.x);

            return;
        }

        RaycastHit2D hit = new();
        if (topLeftHit) hit = topLeftHit;
        else if (topMiddleHit) hit = topMiddleHit;
        else if (topRightHit) hit = topRightHit;

        if (hit)
        {
            verticalVel = Mathf.Min(averageCeilNormal.y * stats.ceilingHitPush, verticalVel);
            shouldApplyGravFallof = true;
            hitCeilingThisFrame = true;

            if (TryGetHitTerrain(hit, out TraversableTerrain terrainTouched)) 
                HandleTerrainTouch(terrainTouched, TerrainInteractionType.Ceiling);
        }
    }

    private void ResetTerrain()
    {
        isBeingSlowed = false;
        isOnWall = false;
        isGrounded = false;
        terrainOn = null;
    }

    #endregion

    #region Wall
    public bool isOnWall { get; private set; }
    private bool isPushingWall;
    private Vector2 wallNormal;

    private bool NormalInWallRange(Vector2 normal) =>
        Mathf.Abs(normal.y) >= 0
        && Mathf.Abs(normal.y) <= stats.wallNormalRange;
    private bool HitGrabbableWall(RaycastHit2D hit) => hit && hit.transform.GetComponent<IWallGrabbable>() != null;

    private void HandleWallDetection(RaycastHit2D topRightHit, RaycastHit2D middleRightHit, RaycastHit2D bottomRightHit, RaycastHit2D topLeftHit, RaycastHit2D middleLeftHit, RaycastHit2D bottomLeftHit)
    {
        TraversableTerrain newTerrain = null;
        Vector2 newWallNormal;
        bool newIsOnWall = false;
        bool canGrabWall = !isGrounded && vel.y < stats.maxYVelForWallGrab;
        isPushingWall = false;

        HandleWallGrab(topRightHit, middleRightHit, 1);
        HandleWallGrab(topLeftHit, middleLeftHit, -1);

        void HandleWallGrab(RaycastHit2D topHit, RaycastHit2D middleHit, int dir)
        {
            RaycastHit2D hit = new();
            if (middleHit) hit = middleHit;
            else if (topHit) hit = topHit;
            else return;

            newWallNormal = hit.normal;
            bool normalInWallRange = NormalInWallRange(newWallNormal);

            if (TryGetHitTerrain(hit, out TraversableTerrain terrainTouched))
                HandleTerrainTouch(terrainTouched, TerrainInteractionType.Wall);

            if (normalInWallRange)
            {
                if (Mathf.RoundToInt(Mathf.Sign(nonZeroVelocityDirection.x)) == dir) isPushingWall = true;

                if (HitGrabbableWall(hit) && canGrabWall)
                {
                    newIsOnWall = true;

                    TryGetHitTerrain(hit, out newTerrain);
                    lastSurfaceType = LastSurfaceType.Wall;

                    wallNormal = newWallNormal;
                }
            }
        }

        HandleTerrainChange(newTerrain, TerrainInteractionType.Wall);
        if (newIsOnWall ^ isOnWall) OnChangeWall(newIsOnWall);

        HandleLedgeGrab();
        void HandleLedgeGrab()
        {
            if (((bottomRightHit && !topRightHit && !middleRightHit) || (bottomLeftHit && !topLeftHit && !middleLeftHit)) && isPushingWall && !isGrounded)
            {
                RaycastHit2D ledgeGrabHit = bottomRightHit ? bottomRightHit : bottomLeftHit;

                //Ledge grabs only on non-wall-grabbable walls for greater control
                if (!HitGrabbableWall(ledgeGrabHit))
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
    }


    private void OnChangeWall(bool newWall)
    {
        if (time < stats.launchGroundWallDetectDelay) return;

        isOnWall = newWall;

        if (isOnWall)
        {
            isJumping = false;
            coyoteUsable = true;
            bufferedJumpUsable = true;
            shouldApplyGravFallof = false;
            isGrounded = false;
            entryLaunchInterrupted = true;
        }
        else
        {
            timeLeftSurface = time;
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

        //Priotity is middle then right then left
        if (bottomMiddleHit) hit = bottomMiddleHit;
        else if (bottomRightHit) hit = bottomRightHit;
        else if (bottomLeftHit) hit = bottomLeftHit;

        Vector2 newGroundNormal = hit.normal;

        if (hit)
        {
            if (TryGetHitTerrain(hit, out TraversableTerrain terrainTouched))
                HandleTerrainTouch(terrainTouched, TerrainInteractionType.Ground);

            if (newGroundNormal.y > stats.minGroundNormal && TryGetHitTerrain(hit, out newTerrain))
            {
                newGrounded = true;
                lastSurfaceType = LastSurfaceType.Ground;
            }
        }
        HandleTerrainChange(newTerrain, TerrainInteractionType.Ground);
        if (isGrounded ^ newGrounded) OnChangeGrounded(newGrounded);
    }


    private void OnChangeGrounded(bool newGrounded)
    {
        if (time < stats.launchGroundWallDetectDelay) return;

        isGrounded = newGrounded;

        if (isGrounded)
        {
            isJumping = false;
            coyoteUsable = true;
            bufferedJumpUsable = true;
            shouldApplyGravFallof = false;
            entryLaunchInterrupted = true;
        }
        else
        {
            timeLeftSurface = time;

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
    private float currentMaxSpeed;
    private float acceleration;
    private float horizontalDelta;

    private void HandleHorizontalMovement()
    {
        float moveInput = HorizontalInput;
        if (isPushingWall && !isJumping)
            horizontalVel = 0;

        float turningMult = Mathf.Lerp(1, stats.turningAccelerationMultiplier 
            //* SlipMultiplier
            , VelocityOpposingMovementStrength(vel));
        float apexBonus = moveInput * stats.apexSpeedIncrease * ApexProximity;

        //used for slower exit from wall
        float wallExitMult = lastSurfaceType == LastSurfaceType.Wall
            ? Mathf.Clamp01((time - timeLeftSurface) / stats.timeToFullSpeedFromWall)
            : 1;

        acceleration = Mathf.Abs(moveInput) > 0
            ? Mathf.Lerp(stats.minAcceleration, stats.maxAcceleration, momentum) * turningMult   //acceleration
            : Mathf.Lerp(stats.minDeceleration, stats.maxDeceleration, momentum);                //deceleration

        currentMaxSpeed = 1 / SlipMultiplier * Mathf.Lerp(stats.minLandSpeed, stats.maxLandSpeed, momentum);
        targetSpeed = moveInput * currentMaxSpeed;

        if (IsInAir)
        {
            targetSpeed = targetSpeed * stats.airSpeedMultiplier + apexBonus;
            acceleration *= stats.airAccelerationMultiplier;
        }
        else acceleration *= SlipMultiplier;   //For faster aceleration in air when slippery

        float speed = Mathf.MoveTowards(horizontalVel, targetSpeed, acceleration * fixedDeltaTime) * wallExitMult;
        horizontalDelta = speed - horizontalVel;
        horizontalVel = speed;
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
    private float timeLeftSurface = float.MinValue;
    private bool isJumping;
    private bool jumpRequested;
    private float timeJumpRequested = float.MinValue;

    float initialHorizontalJumpVel;

    public float ApexProximity
    {
        get
        {
            if (isJumping) return Mathf.InverseLerp(stats.jumpApexRange, 0, Mathf.Abs(vel.y));
            else return 0;
        }
    }

    private bool HasBufferedJump => bufferedJumpUsable && time < timeJumpRequested + stats.jumpBuffer;
    private bool CanUseCoyote => coyoteUsable && !isGrounded && !isOnWall && time < timeLeftSurface + stats.coyoteTime;

    private void HandleJump()
    {
        float sign = Mathf.Sign(initialHorizontalJumpVel);
        float oppositeMovementReduction =
            Mathf.Approximately(sign, -Mathf.Sign(horizontalDelta)) && horizontalDelta != 0
            ? Mathf.Abs(horizontalDelta) : 0;
        wallJumpHorizontalVel = Mathf.MoveTowards(wallJumpHorizontalVel, 0, stats.wallJumpHorizontalDeceleration * fixedDeltaTime + oppositeMovementReduction);

        if (isDashing || isEntryLaunching) return;

        //If jump ended early or is falling, apply gravity fallof
        if ((!isGrounded && !frameInput.JumpHeld && vel.y > 0 && isJumping) || verticalVel < stats.jumpVelocityFallof) shouldApplyGravFallof = true;

        if (!jumpRequested && !HasBufferedJump) return;

        if (isGrounded || isOnWall || CanUseCoyote) ExecuteJump();

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
        initialHorizontalJumpVel = jumpVel.x;

        ApplyMomentum(stats.wallJumpMomentumIncrease);
    }
    private void GroundJump()
    {
        Jumped?.Invoke();

        verticalVel = stats.jumpVelocity;
        ApplyMomentum(stats.jumpMomentumIncrease);
    }

    private void ResetJump()
    {
        timeLeftSurface = float.MinValue;
        timeJumpRequested = float.MinValue;
        jumpRequested = false;
        isJumping = false;
        shouldApplyGravFallof = false;
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
            float gravFallof = shouldApplyGravFallof
                ? stats.gravAfterFalloffMultiplier
                : 1;

            currentGravity = (stats.gravity + apexAntiGravity) * gravFallof;
        }
        if (isDashing)
            verticalVel = dashVel.y = Mathf.MoveTowards(dashVel.y, -stats.maxDownVel, currentGravity * fixedDeltaTime * dashGravMult);
        else if (isEntryLaunching)
            verticalVel = entryLaunchVel.y = Mathf.MoveTowards(entryLaunchVel.y, -stats.maxDownVel, currentGravity * fixedDeltaTime * entryLaunchGravMult);
        else verticalVel = Mathf.MoveTowards(verticalVel, -stats.maxDownVel, currentGravity * fixedDeltaTime);

    }
    #endregion

    #region Dash

    private bool dashRequested;
    private bool isDashing;
    private bool dashUsed;

    private float dashHorizontalVelMult = 1;
    private float dashGravMult = 1;

    private float timeDashed = float.MinValue;
    private bool dashInterrupted;
    private Vector2 dashVel;
    private Vector2 targetDashVel;

    private void HandleDash()
    {
        float timePercent = Mathf.Clamp01((time - timeDashed) / stats.dashDuration);

        if (isBeingSlowed
            || (timePercent == 1 && isDashing) || isOnWall
            || isPushingWall || isGrounded) dashInterrupted = true;

        if (timePercent == 1) isDashing = false;
        if (isGrounded || isOnWall) dashUsed = false;

        bool canDash = !isDashing && !dashInterrupted && !dashUsed;
        if (dashRequested && canDash) ExecuteDash();
        dashRequested = false;

        if (dashInterrupted) ExitDash();
        else if (isDashing)
        {
            dashHorizontalVelMult = stats.dashHorizontalVelMult.Evaluate(timePercent);
            dashGravMult = stats.dashGravMult.Evaluate(timePercent);

            float speedPercent = stats.dashSpeedCurve.Evaluate(timePercent);
            dashVel = targetDashVel * speedPercent;

            //Applies friction if player is trying to oppose dash velocity for greater control
            float xDecel = VelocityOpposingMovementStrength(dashVel) * stats.dashOpposingMovementFriction * Mathf.Abs(dashVel.x);
            dashVel.x = Mathf.MoveTowards(dashVel.x, 0, fixedDeltaTime * xDecel);
        }
    }

    private void ResetDash()
    {
        ExitDash();
        dashInterrupted = false;
        dashRequested = false;
        dashUsed = false;
        timeDashed = float.MinValue;
    }

    private void ExitDash()
    {
        dashInterrupted = false;
        isDashing = false;
        dashVel = Vector2.zero;
        dashHorizontalVelMult = 1;
        dashGravMult = 1;
    }

    private void ExecuteDash()
    {
        isDashing = true;
        timeDashed = time;
        dashUsed = true;

        shouldApplyGravFallof = true;
        verticalVel = 0;

        targetDashVel = stats.dashVel;
        targetDashVel.x *= FacingDirection;

        //Prevents horizontal velocity opposite dash velocity when doing sudden turn dashes
        horizontalVel = FacingDirection * Mathf.Max(horizontalVel * FacingDirection, 0);
    }


    #endregion

    #region Velocty

    private void HandleVelocity()
    {
        float horizontalMult = isEntryLaunching ? entryLaunchControlMult
            : isDashing ? dashHorizontalVelMult : 1;
        float launchVel = isEntryLaunching ? entryLaunchVel.x
            : isDashing? dashVel.x : 0;

        if (isBeingSlowed)
        {
            Vector2 drag = SlowAreaDrag;

            horizontalVel = Mathf.MoveTowards(horizontalVel, 0, drag.x * fixedDeltaTime * Mathf.Abs(horizontalVel));
            verticalVel = Mathf.MoveTowards(verticalVel, 0, drag.y * fixedDeltaTime * Mathf.Abs(verticalVel));
        }

        if (isGrounded && !isJumping && !isEntryLaunching) verticalVel = stats.groundingPush;

        vel = new Vector2
            (horizontalVel * horizontalMult + launchVel + wallJumpHorizontalVel, 
            verticalVel);

        float maxHorizontalSpeed = Mathf.Abs(vel.x);
        if (isEntryLaunching) maxHorizontalSpeed = stats.maxLaunchMoveSpeed;

        vel.x = Mathf.Clamp(vel.x, -maxHorizontalSpeed, maxHorizontalSpeed);

        nonZeroVelocityDirection = new Vector2(
            vel.x != 0 ? vel.x : nonZeroVelocityDirection.x,
            vel.y != 0 ? vel.y : nonZeroVelocityDirection.y)
            .normalized;
    }

    private void ResetAllVelocities()
    {
        horizontalVel = 0;
        horizontalDelta = 0;
        verticalVel = 0;
        entryLaunchVel = Vector2.zero;
        dashVel = Vector2.zero;
    }

    #endregion

    private void ApplyMovement()
    {
        rb.position = position;
        rb.linearVelocity = vel;
    }


    #region Transitions

    public class LandMovementTransition : SuccesfulTransitionData
    {
        public bool EnteredWithDash { get; }
        public ISand EntrySand { get; }
        public Vector2 EntryDir { get; }

        public LandMovementTransition(Vector2 entryDir, bool enteredWithDash, ISand entrySand)
        {
            EntryDir = entryDir;
            EnteredWithDash = enteredWithDash;
            EntrySand = entrySand;
        }
    }
    public Vector2 TargetSandEntryPos { get; private set; }
    public bool SandEntryPosValid { get; private set; }

    private ISand invalidSandDashSand;
    private float timeSandDashRequested = float.MinValue;
    private bool sandDashRequested;
    private bool HasBufferedSandDash => time < stats.sandDashBuffer + timeSandDashRequested;

    private void ResetSandEntryDash()
    {
        timeSandDashRequested = float.MinValue;
        sandDashRequested = false;
    }

    private void UpdateSandDash()
    {
        sandDashRequested = false;
        SandEntryPosValid = false;
        if (time > stats.sameSandTargetDelay) invalidSandDashSand = null;
    }

    private IStateSpecificTransitionData TransitionToSandEntryDash()
    {
        bool canSandDash = HasBufferedSandDash || sandDashRequested;

        Vector2 pos = position;
        float size = stats.burrowDetectionDistance;

        Collider2D[] overlapCols;
        bool searching = frameInput.Move != Vector2.zero;

        Vector2 boxDir = searching ? frameInput.Move : FacingDirection * Vector2.right;

        if(searching)
            overlapCols = Physics2D.OverlapBoxAll(pos + 0.5f * size * boxDir,
                size * Vector2.one,
                Mathf.Rad2Deg * Mathf.Atan2(boxDir.x, boxDir.y),
                sharedStats.sandLayerMask);
        else
            overlapCols = Physics2D.OverlapBoxAll(pos,
                size * 2 * Vector2.one,
                Mathf.Rad2Deg * Mathf.Atan2(boxDir.x, boxDir.y),
                sharedStats.sandLayerMask);


        for (int i = 0; i < overlapCols.Length; i++)
        {
            Collider2D col = overlapCols[i];

            if (col.transform.TryGetComponent(out ISand sand) && sand.IsBurrowable && sand != invalidSandDashSand)
            {
                Vector2 point = col.ClosestPoint(pos);
                float dist = (point - pos).sqrMagnitude;

                if (col != null)
                {
                    bool isBurrowSand = sand is BurrowSand;
                    float maxDist = isBurrowSand ? stats.burrowDetectionDistance : stats.sandDetectionDistance;

                    if (dist < maxDist * maxDist)
                    {
                        Vector2 overShoot = (sand is SandBall)
                            ? Vector2.zero
                            : frameInput.Move * stats.sandOvershoot;
                        Vector2 queryPos = pos + overShoot;

                        Vector2 entryPoint = col.ClosestPoint(queryPos);
                        Vector2 diff = entryPoint - pos;
                        float entryDist = diff.magnitude;
                        Vector2 dir = diff / dist;

                        RaycastHit2D hit = Physics2D.Raycast(
                            pos, dir, entryDist + 1,
                            sharedStats.collisionLayerMask);

                        if (hit && hit.transform == col.transform)
                        {
                            SandEntryPosValid = true;
                            TargetSandEntryPos = hit.point;

                            if (canSandDash)
                            {
                                isFacingRight = Mathf.Sign(dir.x) == 1;
                                Vector2 heightAdjustedEntryPoint = hit.point + -2 * PlayerHalfHeight * hit.normal;
                                if (sand is SandBall)
                                    heightAdjustedEntryPoint += -hit.normal * col.bounds.size.x;

                                return new SandEntryMovement.SandEntryData(heightAdjustedEntryPoint, sand);
                            }
                        }
                        else Debug.DrawLine(pos, entryPoint, Color.red);
                    }
                }
            }
        }

        return failedData;
    }

    private IStateSpecificTransitionData TransitionToDirectSandEntryDash()
    {
        if (!isEntryLaunching || entryLaunchProgress > stats.entryLaunchPercentToChain) 
            return failedData;

        Vector2 castVector = FrameDisplacement * 2;

        float dist = castVector.magnitude;
        Vector2 dir = castVector / dist;

        RaycastHit2D hit = Physics2D.BoxCast
            (position, col.bounds.size,
            Mathf.Rad2Deg * Mathf.Atan2(dir.x, dir.y),
            dir, dist, sharedStats.collisionLayerMask);
        Vector2 diff = hit.point - position;

        if (hit && hit.transform.TryGetComponent(out ISand sand) && sand.IsBurrowable && sand != invalidSandDashSand)
        {
            if (Vector2.Dot(initialLaunchVel.normalized, diff/hit.distance) > 0.5f)
            {
                Debug.DrawLine(position, hit.point, Color.green, 100);
                Vector2 entryPoint = hit.point + dir * (PlayerHalfHeight + 0.5f);
                return new SandEntryMovement.SandEntryData(entryPoint, sand);
            }
        }

        return failedData;
    }

    private IStateSpecificTransitionData TransitionToSandEntry()
    {
        if (isEntryLaunching) return failedData;

        Vector2 dir = Vector2.down;

        RaycastHit2D hit = Physics2D.BoxCast(position, col.bounds.size, 0,
            dir, stats.groundedDistance, sharedStats.collisionLayerMask);

        if (hit && hit.transform.TryGetComponent(out ISand sand) && sand != invalidSandDashSand && sand is BurrowSand)
        {
            Vector2 entryPoint = hit.point + dir * (PlayerHalfHeight + 0.5f);
            SandEntryPosValid = true;
            TargetSandEntryPos = hit.point;

            if (frameInput.SandDashDown && sand.IsBurrowable)
                return new BurrowMovement.BurrowMovementTransitionData(dir, entryPoint, sand);
        }

        return failedData;
    }

    #endregion

    #region Collisions
    public void TriggerEnter(IPlayerCollisionListener collisionListener)
    {
        if (collisionListener == null) return;

        if (collisionListener is SlowingArea)
        {
            isBeingSlowed = true;
            dashInterrupted = true;
            ApplyMomentum(-stats.slowAreaMomentumLoss);
        }
    }
    public void TriggerExit(IPlayerCollisionListener collisionListener)
    {
        if (collisionListener == null) return;
        if (collisionListener is SlowingArea) isBeingSlowed = false;
    }

    #endregion
}