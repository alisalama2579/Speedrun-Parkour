using System;
using System.Collections;
using System.Collections.Generic;
using System.Persistence;
using UnityEngine;
using static TransitionLibrary;

public class LandMovement : IMovementState
{
    private readonly MovementStatsHolder sharedStats;
    private readonly LandMovementStats stats;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;

    private float PlayerHalfWidth => col.bounds.extents.x;
    private float HalfHeight => col.bounds.extents.y;

    public LandMovement(Rigidbody2D rb, Collider2D col, MovementStatsHolder stats)
    {
        this.col = col;
        sharedStats = stats;
        this.stats = stats.landStats;
        this.rb = rb;
    }

    public void InitializeTransitions(PlayerStateMachine controller)
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

            entrySand = transitionData.EntrySand;
            float launchSpeed = transitionData.EnteredWithDash
                ? entrySand.LaunchSpeed
                : entrySand.WeakLaunchSpeed;

            ExecuteEntryLaunch(launchSpeed * transitionData.EntryDir);
        }
    }

    public void ExitState()
    {
        time = 0;
        frameInput = new();

        ResetAllVelocities();
        ResetEntryLaunch();
        ResetLeap();
        ResetTerrain();
        ResetJump();
        ResetSandEntryDash();
    }

    private MovementInput frameInput;
    private float HorizontalInput => frameInput.SnappedHorizontalMove;
    private float fixedDeltaTime;
    private float time;

    public void Update(MovementInput frameInput)
    {
        time += Time.deltaTime;
        HandleInput(frameInput);
    }

    public void HandleInput(MovementInput frameInput)
    {
        this.frameInput = frameInput;

        if (HorizontalInput == 1) isFacingRight = true;
        if (HorizontalInput == -1) isFacingRight = false;

        //Only changes dash requested if DashDown
        if (frameInput.DashDown) leapRequested = true;
        if (frameInput.SandDashDown) timeSandDashRequested = time;
        if (frameInput.JumpDown)
        {
            jumpRequested = true;
            timeJumpRequested = time;
        }
    }


    public void FixedUpdate()
    {
        fixedDeltaTime = Time.fixedDeltaTime;
        pos = rb.position;

        HandleCollisionInteractions();
        HandleJump();
        HandleGravity();
        HandleLeap();
        HandleEntryLaunch();
        HandleHorizontalMovement();
        HandleMomentum();
        HandleVelocity();
        ApplyMovement();
    }

    private Vector2 pos;
    public Vector2 vel;
    private Vector2 nonZeroVelDir;

    private float wallJumpHorizontalVel;
    private float horizontalVel;
    private float verticalVel;

    private bool isFacingRight;

    #region Public Properties
    public float VerticalVel => verticalVel;
    public float HorizontalVel => horizontalVel;
    public bool IsFacingRight => isFacingRight;
    public int FacingDirection => isFacingRight ? 1 : -1;
    public Vector2 NonZeroVelDir => nonZeroVelDir;
    private Vector2 FrameDisplacement => vel * fixedDeltaTime;
    public Vector2 Vel => vel;
    public Vector2 Pos => pos;
    public bool IsLeaping => isLeaping;
    public bool IsEntryLaunching => isEntryLaunching;
    #endregion

    /// <summary>
    /// Between 0 and 1, how strongly the desired input opposes with the current velocity
    /// </summary>
    private float VelocityOpposingMovementStrength(Vector2 vel) => Mathf.Clamp01(1 - Mathf.Abs(vel.normalized.x + FacingDirection));

    #region ExternalEffectors
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
    public event Action<Vector2> OnEntryLaunch;

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
         || isLeaping || hitCeilingThisFrame || isPushingWall)
            entryLaunchInterrupted = true;

        if ((hitCeilingThisFrame || isPushingWall) && isEntryLaunching)
            Debug.Log("Entry launch hit something");

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
        OnEntryLaunch?.Invoke(launchVel);

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
            EventsHolder.PlayerEvents.OnPlayerGrabWall?.Invoke(newTerrain);
        if (interactionType == TerrainInteractionType.Ground)
            EventsHolder.PlayerEvents.OnPlayerLandOnGround?.Invoke(newTerrain);

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
        Vector2 downOrigin = Vector2.down * (HalfHeight - stats.skinWidth);
        HandleWallDetection(
        Physics2D.Raycast(pos - downOrigin + rightOrigin, Vector2.right, stats.wallDetectionDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(pos + rightOrigin, Vector2.right, stats.wallDetectionDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(pos + downOrigin + rightOrigin, Vector2.right, stats.wallDetectionDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(pos - downOrigin - rightOrigin, Vector2.left, stats.wallDetectionDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(pos - rightOrigin, Vector2.left, stats.wallDetectionDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(pos + downOrigin - rightOrigin, Vector2.left, stats.wallDetectionDistance, sharedStats.collisionLayerMask)
        );
        HandleGround(
        Physics2D.Raycast(pos + downOrigin - rightOrigin, Vector2.down, stats.groundedDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(pos + downOrigin + rightOrigin, Vector2.down, stats.groundedDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(pos + downOrigin, Vector2.down, stats.groundedDistance, sharedStats.collisionLayerMask)
        );
        HandleCeiling(
        Physics2D.Raycast(pos - downOrigin - rightOrigin, Vector2.up, stats.ceilingDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(pos - downOrigin + rightOrigin, Vector2.up, stats.ceilingDistance, sharedStats.collisionLayerMask),
        Physics2D.Raycast(pos - downOrigin, Vector2.up, stats.ceilingDistance, sharedStats.collisionLayerMask)
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
            if (topLeftHit) ceilingNudgeDir = (topLeftHit.point - pos).normalized;
            if (topRightHit) ceilingNudgeDir = (topRightHit.point - pos).normalized;
            pos += ceilingNudgeDir * Mathf.Sign(nonZeroVelDir.x);

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
    public event Action<bool, TraversableTerrain> OnPlayerChangedWall;
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
                if (vel.x * dir > stats.minVelForPushingWall) isPushingWall = true;

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
                    float dist = Mathf.Abs(ledgeGrabHit.point.x - (pos.x + dir * PlayerHalfWidth));

                    Vector2 ledgeNormal = ledgeGrabHit.normal;

                    if (dist < stats.ledgeGrabDistance && NormalInWallRange(ledgeNormal))
                    {
                        verticalVel = Mathf.Max(0, verticalVel);

                        Vector2 ledgeGrabDir = (pos - bottomRightHit.point).normalized;
                        ledgeGrabDir.x *= -dir;
                        ledgeGrabDir.y *= dir;

                        pos += ledgeGrabDir;
                    }
                }
            }
        }
    }


    private void OnChangeWall(bool newWall)
    {
        if (time < stats.launchGroundWallDetectDelay) return;

        isOnWall = newWall;
        OnPlayerChangedWall?.Invoke(newWall, terrainOn);

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
    public event Action<bool, float, TraversableTerrain> OnChangeGround;

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

        float impact = 1;
        OnChangeGround?.Invoke(newGrounded, impact, terrainOn);

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

    public event Action<float, TraversableTerrain> OnPlayerMove;

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

        bool hasMoveInput = Mathf.Abs(moveInput) > 0;

        acceleration = hasMoveInput
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

        if (hasMoveInput) OnPlayerMove?.Invoke(horizontalVel, terrainOn);
    }

    #endregion

    #region Jump
    public event Action<TraversableTerrain> OnJump;
    public event Action<TraversableTerrain> OnWallJump;

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

        if (isLeaping || isEntryLaunching) return;

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
        OnJump?.Invoke(terrainOn);
        Vector2 jumpVel = stats.wallJumpVel * Vector2.LerpUnclamped(wallNormal, Vector2.up, stats.wallJumpUpBias);

        verticalVel = jumpVel.y;
        wallJumpHorizontalVel = jumpVel.x;
        initialHorizontalJumpVel = jumpVel.x;

        ApplyMomentum(stats.wallJumpMomentumIncrease);
    }
    private void GroundJump()
    {
        OnWallJump?.Invoke(terrainOn);

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
    private void HandleGravity()
    {
        float grav;

        if (isGrounded) return;
        if (isOnWall) grav = stats.onWallGravity;
        else
        {
            float apexAntiGravity = Mathf.Lerp(0, -stats.apexAntigravity, ApexProximity);
            float gravFallof = shouldApplyGravFallof
                ? stats.gravAfterFalloffMultiplier
                : 1;

            grav = (stats.gravity + apexAntiGravity) * gravFallof;
        }
        if (isLeaping)
            verticalVel = leapVel.y = Mathf.MoveTowards(leapVel.y, -stats.maxDownVel, grav * fixedDeltaTime * leapGravMult);
        else if (isEntryLaunching)
            verticalVel = entryLaunchVel.y = Mathf.MoveTowards(entryLaunchVel.y, -stats.maxDownVel, grav * fixedDeltaTime * entryLaunchGravMult);
        else verticalVel = Mathf.MoveTowards(verticalVel, -stats.maxDownVel, grav * fixedDeltaTime);

    }
    #endregion

    #region Leap
    public event Action OnLeap;

    private bool leapRequested;
    private bool isLeaping;
    private bool leapUsed;

    private float leapHorizontalVelMult = 1;
    private float leapGravMult = 1;

    private float timeLeaped = float.MinValue;
    private bool leapInterrupted;
    private Vector2 leapVel;
    private Vector2 targetLeapVel;

    private void HandleLeap()
    {
        float timePercent = Mathf.Clamp01((time - timeLeaped) / stats.leapDuration);

        if (isBeingSlowed
            //|| (timePercent == 1 && isDashing) 
            || isOnWall
            || isPushingWall || isGrounded) leapInterrupted = true;

        if (isGrounded || isOnWall) leapUsed = false;

        bool canDash = !isLeaping && !leapInterrupted && !leapUsed;
        if (leapRequested && canDash) ExecuteLeap();
        leapRequested = false;

        if (leapInterrupted) ExitLeap();
        else if (isLeaping)
        {
            leapHorizontalVelMult = stats.leapHorizontalVelMult.Evaluate(timePercent);
            leapGravMult = stats.leapGravCurve.Evaluate(timePercent) * stats.leapGravMult;

            float speedPercent = stats.leapSpeedCurve.Evaluate(timePercent);
            leapVel.x = targetLeapVel.x * speedPercent;

            //Applies friction if player is trying to oppose dash velocity for greater control
            float xDecel = VelocityOpposingMovementStrength(leapVel) * stats.leapOpposingMovementFriction * Mathf.Abs(leapVel.x);
            leapVel.x = Mathf.MoveTowards(leapVel.x, 0, fixedDeltaTime * xDecel);
        }
    }

    private void ResetLeap()
    {
        ExitLeap();
        leapInterrupted = false;
        leapRequested = false;
        leapUsed = false;
        timeLeaped = float.MinValue;
    }

    private void ExitLeap()
    {
        leapInterrupted = false;
        isLeaping = false;
        leapVel = Vector2.zero;
        leapHorizontalVelMult = 1;
        leapGravMult = 1;
    }

    private void ExecuteLeap()
    {
        OnLeap?.Invoke();

        isLeaping = true;
        timeLeaped = time;
        leapUsed = true;

        shouldApplyGravFallof = true;

        targetLeapVel = stats.leapVel;
        targetLeapVel.x *= FacingDirection;
        leapVel.y = targetLeapVel.y;
        verticalVel = 0;

        //Prevents horizontal velocity opposite dash velocity when doing sudden turn dashes
        horizontalVel = FacingDirection * Mathf.Max(horizontalVel * FacingDirection, 0);
    }


    #endregion

    #region Velocty
    private void HandleVelocity()
    {
        float horizontalMult = isEntryLaunching ? entryLaunchControlMult
            : isLeaping ? leapHorizontalVelMult : 1;
        float launchVel = isEntryLaunching ? entryLaunchVel.x
            : isLeaping ? leapVel.x : 0;

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

        nonZeroVelDir = new Vector2(
            vel.x != 0 ? vel.x : nonZeroVelDir.x,
            vel.y != 0 ? vel.y : nonZeroVelDir.y)
            .normalized;
    }

    private void ResetAllVelocities()
    {
        horizontalVel = 0;
        horizontalDelta = 0;
        verticalVel = 0;
        entryLaunchVel = Vector2.zero;
        leapVel = Vector2.zero;
    }

    #endregion

    private void ApplyMovement()
    {
        rb.position = pos;
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

    private ISand entrySand;
    private float timeSandDashRequested = float.MinValue;
    private void ResetSandEntryDash()
    {
        timeSandDashRequested = float.MinValue;
        SandEntryPosValid = false;
    }

    private bool HasBufferedSandDash => time < stats.sandDashBuffer + timeSandDashRequested;
    ISand InvalidSandDashSand => time > stats.sameSandTargetDelay ? null : entrySand;

    private IStateSpecificTransitionData TransitionToSandEntryDash()
    {
        SandEntryPosValid = false;

        bool viablePointFound = false;
        bool canSandDash = frameInput.SandDashDown || HasBufferedSandDash;
        RaycastHit2D targetHit = new();
        ISand targetSand = null;

        float detectDist = stats.sandDetectionDistance;
        Vector2 lookDir = frameInput.NonZeroLook;
        Vector2 pos = this.pos;
        Vector2 origin = pos + 0.5f * detectDist * lookDir;

        #region Direction Search

        Vector2 boxBounds = new Vector2(stats.directionDetectionSize, detectDist);
        CheckColliders(
            Physics2D.OverlapBoxAll(origin,
            boxBounds,
            Vector2Utility.GetVector2Angle(lookDir),
            sharedStats.sandLayerMask)
            );

        Utility.DrawBox(origin, boxBounds, lookDir, viablePointFound ? Color.blue : Color.black);

        #endregion

        #region Area Search

        if (!viablePointFound)
        {
            CheckColliders(
                Physics2D.OverlapBoxAll(origin,
                detectDist * Vector2.one,
                Vector2Utility.GetVector2Angle(lookDir),
                sharedStats.sandLayerMask)
                );

            Utility.DrawBox(origin, detectDist * Vector2.one, lookDir, Color.cyan);
        }
        #endregion

        void CheckColliders(Collider2D[] cols)
        {
            for (int i = 0; i < cols.Length; i++)
            {
                Collider2D col = cols[i];

                if (ColliderIsBurrowable(col, out ISand sand))
                {
                    Vector2 point = col.ClosestPoint(pos);
                    float dist = (point - pos).magnitude;

                    if (dist <= MaxDist(sand))
                    {
                        Vector2 overShoot = (sand is SandBall)
                            ? Vector2.zero
                            : dist * stats.sandOvershoot * frameInput.Look / MaxDist(sand);
                        Vector2 queryPos = pos + overShoot;

                        Vector2 entryPoint = col.ClosestPoint(queryPos);
                        Vector2 diff = entryPoint - pos;
                        float entryDist = diff.magnitude;
                        Vector2 dir = diff / dist;

                        RaycastHit2D hit = CollisionIntersectionRay(dir, entryDist);
                        if (hit && hit.transform == col.transform)
                        {
                            targetHit = hit;
                            targetSand = sand;
                        }
                        else Debug.DrawLine(pos, entryPoint, Color.red);
                    }
                }
            }
        }


        if (targetHit && targetSand != null)
        {
            SandEntryPosValid = true;
            TargetSandEntryPos = targetHit.point;

            if (canSandDash)
            {
                //Face right or left depending on direction of sand
                isFacingRight = Mathf.Sign((targetHit.point - pos).x) == 1;

                Vector2 heightAdjustedEntryPoint = HeightAdjustedEntryPoint(targetHit.point, -targetHit.normal);
                return new SandEntryMovement.SandEntryData(heightAdjustedEntryPoint, targetSand);
            }
        }

        return failedData;

        bool ColliderIsBurrowable(Collider2D col, out ISand sand) => col.transform.TryGetComponent(out sand) && SandCanBeEntered(sand);
        float MaxDist(ISand sand) => sand is BurrowSand ? stats.sandDetectionDistance : stats.sandDashDetectionDistance;
        RaycastHit2D CollisionIntersectionRay(Vector2 dir, float dist) => Physics2D.Raycast(pos, dir, dist + 1, sharedStats.collisionLayerMask);
    }

    private bool SandCanBeEntered(ISand sand) => sand.IsBurrowable && sand != InvalidSandDashSand;

    private IStateSpecificTransitionData TransitionToDirectSandEntryDash()
    {
        if ((!isEntryLaunching || entryLaunchProgress > stats.entryLaunchPercentToChain)) 
            return failedData;

        Vector2 castVector = stats.directSandDashDetectionDistance * FrameDisplacement.normalized + FrameDisplacement;

        float dist = castVector.magnitude;
        Vector2 dir = castVector / dist;

        RaycastHit2D hit = Physics2D.BoxCast
            (pos, col.bounds.size, Vector2Utility.GetVector2Angle(dir),
            dir, dist, sharedStats.collisionLayerMask);

        if (hit && hit.transform.TryGetComponent(out ISand sand) && sand is BurrowSand && SandCanBeEntered(sand))
        {
            Debug.DrawLine(pos, pos + castVector * hit.distance, Color.green, 100);
            return new SandEntryMovement.SandEntryData(HeightAdjustedEntryPoint(pos + castVector * hit.distance, dir), sand);
        }

        return failedData;
    }

    private IStateSpecificTransitionData TransitionToSandEntry()
    {
        if (isEntryLaunching) return failedData;

        Vector2 dir = Vector2.down;

        RaycastHit2D hit = Physics2D.BoxCast(pos, col.bounds.size, 0,
            dir, stats.groundedDistance, sharedStats.collisionLayerMask);

        if (hit && hit.transform.TryGetComponent(out ISand sand) && SandCanBeEntered(sand) && sand is BurrowSand)
        {
            Vector2 entryPoint = HeightAdjustedEntryPoint(hit.point, dir);
            SandEntryPosValid = true;
            TargetSandEntryPos = hit.point;

            if (frameInput.SandDashDown && sand.IsBurrowable)
                return new BurrowMovement.BurrowMovementTransitionData(dir, entryPoint, sand);
        }

        return failedData;
    }

    private Vector2 HeightAdjustedEntryPoint(Vector2 point, Vector2 dir) => point + 2 * HalfHeight * dir;
    #endregion

    #region Collisions
    public void TriggerEnter(IPlayerCollisionListener collisionListener)
    {
        if (collisionListener == null) return;

        if (collisionListener is SlowingArea)
        {
            isBeingSlowed = true;
            leapInterrupted = true;
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