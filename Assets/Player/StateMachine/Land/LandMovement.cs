using UnityEngine;
using System;
using static TransitionLibrary;

public class LandMovement : IMovementState
{
    #region State
    private readonly MovementStatsHolder sharedStats;
    private readonly LandMovementStats stats;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;

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
        controller.AddTransition(GetType(), typeof(BurrowMovement), TransitionToDirectSandEntryDash);
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
        ResetRoll();
        ResetEntryLaunch();
        ResetLeap();
        ResetTerrain();
        ResetJump();
        ResetSandEntryDash();
    }
    #endregion
    #region Input
    private MovementInput frameInput;
    private float HorizontalInput => frameInput.SnappedHorizontalMove;
    private float fixedDeltaTime;
    private float time;
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
    #endregion

    public void Update(MovementInput frameInput)
    {
        time += Time.deltaTime;
        HandleInput(frameInput);
    }


    public void FixedUpdate()
    {
        fixedDeltaTime = Time.fixedDeltaTime;
        pos = rb.position;

        HandleExternalEffectors();
        HandleCollisionInteractions();
        HandleJump();
        HandleGravity();
        HandleLeap();
        HandleEntryLaunch();
        HandleHorizontalMovement();
        HandleRoll();
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
    public bool IsJumping => isJumping;
    public bool IsRolling => isRolling;
    #endregion
    #region Private properties
    /// <summary>
    /// Between 0 and 1, how strongly the desired input opposes with the current velocity
    /// </summary>
    private float VelocityOpposingMovementStrength(float vel) => Mathf.Clamp01(1 - Mathf.Abs(Mathf.Sign(vel) + FacingDirection));
    private float HalfWidth => col.bounds.extents.x;
    private float HalfHeight => col.bounds.extents.y;
    #endregion

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

    private void HandleExternalEffectors()
    {
        wasOnSlipperyGround = lastNonAirSurface?.terrain is SlipperyGround;
    }

    #endregion

    #region TerrainBehaviour

    private class Surface
    {
        public TraversableTerrain terrain;
        public TerrainSurfaceType surfaceType;
        public Surface(TraversableTerrain terrain, TerrainSurfaceType surfaceType)
        {
            this.terrain = terrain;
            this.surfaceType = surfaceType;
        }

        public override bool Equals(object obj)
        {
            return obj is Surface surface && this == surface;
        }
        public static bool operator == (Surface lhs, Surface rhs)
        {
            bool leftNull = lhs is null;
            bool rightNull = rhs is null;
            if (leftNull || rightNull)
                return leftNull == rightNull;

            return lhs.terrain == rhs.terrain &&
                   lhs.surfaceType == rhs.surfaceType;
        }
        public static bool operator !=(Surface lhs, Surface rhs) => !(lhs == rhs);
    }
    Surface lastNonAirSurface;

    private void HandleTerrainInteractionUpdate(TraversableTerrain newTerrain, TerrainSurfaceType surfaceType)
    {
        if (newTerrain == null) return;

        Interact(newTerrain, surfaceType);
        lastNonAirSurface = new Surface(newTerrain, surfaceType);

        void Interact(TraversableTerrain terrain, TerrainSurfaceType surfaceType)
        {
            if (TryGetTerrainInteractor(terrain, out TraversalInteractionComponent interactor))
                interactor.OnInteract(new TerrainInteract(CollisionType.Stayed, surfaceType));
        }
    }

    private void HandleTerrainTouch(TraversableTerrain terrainTouched, CollisionType collisionType, TerrainSurfaceType surfaceType)
    {
        bool interactorInvalid = !TryGetTerrainInteractor(terrainTouched, out TraversalInteractionComponent interactor);
        if (interactorInvalid) return;

        interactor.OnInteract(new TerrainTouch(collisionType, surfaceType));
    }

    /// <summary>
    /// Returns true if hit transform has component TraversableTerrain and changes terrain refrence accordingly
    /// </summary>
    private bool TryGetHitTerrain(RaycastHit2D hit, out TraversableTerrain terrain)
    {
        terrain = null;
        return hit ? hit.transform.TryGetComponent(out terrain) : false;
    }
    private bool TryGetTerrainInteractor(TraversableTerrain terrain, out TraversalInteractionComponent interactor)
    {
        interactor = null;
        return terrain != null && terrain.TryGetComponent(out interactor);
    }
    private bool HitInvalidDueToEntryLaunch(RaycastHit2D hit) => !hit || (hit.transform.TryGetComponent(out ISand sand) && sand == entrySand && time <= stats.launchGroundWallDetectDelay);

    private void HandleCollisionInteractions()
    {
        Vector2 rightOrigin = Vector2.right * (HalfWidth - stats.skinWidth);
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
            if (TryGetHitTerrain(hit, out TraversableTerrain terrainTouched))
                HandleTerrainTouch(terrainTouched, CollisionType.Stayed, TerrainSurfaceType.Ceiling);

            if (hit.collider.enabled && !HitInvalidDueToEntryLaunch(hit))
            {
                verticalVel = Mathf.Min(averageCeilNormal.y * stats.ceilingHitPush, verticalVel);
                shouldApplyGravFallof = true;
                hitCeilingThisFrame = true;
            }
        }
    }

    private void ResetTerrain()
    {
        isBeingSlowed = false;
        isOnWall = false;
        isGrounded = false;
        lastNonAirSurface = null;
        lastNonAirSurface = null;
    }

    #endregion

    #region Wall
    public event Action<bool> OnPlayerChangedWall;
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

            if (!HitInvalidDueToEntryLaunch(hit))
            {
                if (normalInWallRange)
                {
                    if (TryGetHitTerrain(hit, out TraversableTerrain terrainTouched))
                        HandleTerrainTouch(terrainTouched, CollisionType.Stayed, TerrainSurfaceType.Wall);

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
        }

        HandleTerrainInteractionUpdate(newTerrain, TerrainSurfaceType.Wall);
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
                    float dist = Mathf.Abs(ledgeGrabHit.point.x - (pos.x + dir * HalfWidth));

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
        OnPlayerChangedWall?.Invoke(newWall);

        if (isOnWall)
        {
            isJumping = false;
            coyoteUsable = true;
            bufferedJumpUsable = true;
            shouldApplyGravFallof = false;
            isGrounded = false;
        }
        else
        {
            timeLeftSurface = time;
            if (!isJumping) shouldApplyGravFallof = true;
        }
    }

    #endregion

    #region Ground
    public event Action<bool, float> OnChangeGround;

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
            if (!HitInvalidDueToEntryLaunch(hit))
            {
                if (newGroundNormal.y > stats.minGroundNormal && TryGetHitTerrain(hit, out newTerrain))
                {
                    newGrounded = true;
                    lastSurfaceType = LastSurfaceType.Ground;
                }
            }
        }
        HandleTerrainInteractionUpdate(newTerrain, TerrainSurfaceType.Ground);
        if (isGrounded ^ newGrounded) OnChangeGrounded(newGrounded);
    }


    private void OnChangeGrounded(bool newGrounded)
    {
        isGrounded = newGrounded;

        float impact = 1;
        OnChangeGround?.Invoke(newGrounded, impact);

        if (isGrounded)
        {
            if(isLeaping || isEntryLaunching && Mathf.Approximately(Mathf.Sign(vel.x), HorizontalInput) && HorizontalInput != 0)
                rollRequested = true;

            EventsHolder.PlayerEvents.OnPlayerLandOnGround?.Invoke(lastNonAirSurface.terrain);

            isJumping = false;
            coyoteUsable = true;
            bufferedJumpUsable = true;
            shouldApplyGravFallof = false;
        }
        else
        {
            timeLeftSurface = time;

            //Applies gravity fallof if not jumping, such as walking off a ledge
            if (!isJumping) shouldApplyGravFallof = true;
        }
    }

    #endregion

    #region EntryLaunch
    public event Action<Vector2> OnEntryLaunch;

    private bool isEntryLaunching;
    private bool entryLaunchInterrupted;
    private float entryLaunchProgress;

    private Vector2 entryLaunchVel;

    private float entryLaunchGravMult;
    private float entryLaunchControlMult;

    private void HandleEntryLaunch()
    {
        entryLaunchProgress = Mathf.Clamp01(time / stats.launchMultDuration);

        if (isBeingSlowed
         || isLeaping || hitCeilingThisFrame || isPushingWall || isGrounded)
            entryLaunchInterrupted = true;

        if ((hitCeilingThisFrame || isPushingWall) && isEntryLaunching)
            Debug.Log("Entry launch hit something");

        if (entryLaunchInterrupted) ExitEntryLaunch();
        else if (isEntryLaunching)
        {
            entryLaunchGravMult = stats.launchGravCurve.Evaluate(entryLaunchProgress);
            entryLaunchControlMult = stats.launchControlCurve.Evaluate(entryLaunchProgress);

            bool horizontalDeltaOpposing = Mathf.Approximately(Mathf.Sign(entryLaunchVel.x), -Mathf.Sign(horizontalDelta)) && horizontalDelta != 0;
            float oppDecel = horizontalDeltaOpposing ? Mathf.Abs(horizontalDelta) * stats.launchOpposingMovementFriction : 0;
            float xDecel = oppDecel + stats.launchDecel;

            entryLaunchVel.x = Mathf.MoveTowards(entryLaunchVel.x, 0, fixedDeltaTime * xDecel);
        }
    }

    private void ExecuteEntryLaunch(Vector2 launchVel)
    {
        OnEntryLaunch?.Invoke(launchVel);

        entryLaunchVel = launchVel;
        verticalVel = 0;

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

    public event Action<float> OnPlayerMove;
    private float horizontalDelta;

    private void HandleHorizontalMovement()
    {
        if (isRolling)
            return;

        if (isPushingWall && !isJumping)
            horizontalVel = 0;

        float moveInput = HorizontalInput;
        bool hasMoveInput = Mathf.Abs(moveInput) > 0;

        float turningMult = GetTurningMult();
        float apexBonus = GetApexBonus();
        float acceleration = GetAcceleration();
        float targetSpeed = GetTargetSpeed();
        float vel = GetFinalVel();

        SetHorizontalVel(vel);
        if (hasMoveInput) OnPlayerMove?.Invoke(horizontalVel);

        float GetTurningMult() { 
            return Mathf.Lerp(1, stats.turningAccelerationMultiplier, VelocityOpposingMovementStrength(horizontalVel)); 
        }
        float GetApexBonus(){
            if (isJumping) return stats.apexSpeedIncrease * ApexProximity;
            else return 0;
        }
        float GetAcceleration(){
            float acceleration = hasMoveInput 
                ? Mathf.Lerp(stats.minAcceleration, stats.maxAcceleration, momentum) * turningMult  //acceleration
                : Mathf.Lerp(stats.minDeceleration, stats.maxDeceleration, momentum); //deceleration

            if (IsInAir) acceleration *= stats.airAccelerationMultiplier;
            else acceleration *= SlipMultiplier;

            return acceleration;
        }
        float GetTargetSpeed(){
            float targetSpeed = Mathf.Lerp(stats.minLandSpeed, stats.maxLandSpeed, momentum);
            if (wasOnSlipperyGround) targetSpeed *= 1 / SlipMultiplier;
            targetSpeed *= moveInput;

            return targetSpeed;
        }
        float GetFinalVel(){
            float vel = Mathf.MoveTowards(horizontalVel, targetSpeed, acceleration * fixedDeltaTime);

            if(lastSurfaceType == LastSurfaceType.Wall) vel *= Mathf.Clamp01((time - timeLeftSurface) / stats.timeToFullSpeedFromWall);
            return vel;
        }
    }
    private void SetHorizontalVel(float vel)
    {
        horizontalDelta = vel - horizontalVel;
        horizontalVel = vel;
    }

    #endregion

    #region Roll

    public event Action<float> OnRoll;
    private bool isRolling;
    private float timeRolled;
    private float rollDir;
    private float rollSpeed;
    private bool rollRequested;
    private bool rollInterrupted;

    private void HandleRoll()
    {
        if (rollRequested) ExecuteRoll();
        rollRequested = false;

        if ((isRolling && (time > timeRolled + stats.rollDuration)) ||
            isPushingWall || isJumping
            || Mathf.Approximately(HorizontalInput, -rollDir)){ rollInterrupted = true;  }

        if (rollInterrupted)
            ExitRoll();
        else if (isRolling)
            SetHorizontalVel(rollSpeed * rollDir);
    }
    private void ExecuteRoll()
    {
        isRolling = true; 
        timeRolled = time;

        float sign = Mathf.Sign(vel.x);
        rollSpeed = vel.x/sign + stats.rollSpeed; 
        rollDir = sign; 
        OnRoll?.Invoke(rollDir);

    }
    private void ResetRoll()
    {
        ExitRoll();
        timeRolled = float.MinValue;
    }
    private void ExitRoll()
    {
        isRolling = false;
        rollInterrupted = false;

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
        OnWallJump?.Invoke(lastNonAirSurface?.terrain);
        Vector2 jumpVel = stats.wallJumpVel * Vector2.LerpUnclamped(wallNormal, Vector2.up, stats.wallJumpUpBias);

        verticalVel = jumpVel.y;
        wallJumpHorizontalVel = jumpVel.x;
        initialHorizontalJumpVel = jumpVel.x;

        ApplyMomentum(stats.wallJumpMomentumIncrease);
    }
    private void GroundJump()
    {
        OnJump?.Invoke(lastNonAirSurface?.terrain);

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

    private void HandleLeap()
    {
        float timePercent = Mathf.Clamp01((time - timeLeaped) / stats.leapDuration);

        if (isBeingSlowed
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

            //Applies friction if player is trying to oppose dash velocity for greater control
            float xDecel = VelocityOpposingMovementStrength(leapVel.x) * stats.leapOpposingMovementFriction * Mathf.Abs(leapVel.x);
            leapVel.x = Mathf.MoveTowards(leapVel.x, 0, fixedDeltaTime * (xDecel + stats.leapDecel));
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

        float persistingLaunchVel = Mathf.Max(entryLaunchVel.x * FacingDirection, 0)  * stats.leapEntryLaunchRetainPercent;

        leapVel = new Vector2((stats.leapVel.x + persistingLaunchVel) * FacingDirection, stats.leapVel.y);
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
        Vector2 pos = this.pos + vel * fixedDeltaTime;
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

                    Debug.DrawRay(point, Vector2.up * 5, Color.red);

                    if (dist <= MaxDist(sand))
                    {
                        float downVel = verticalVel < 0 && frameInput.Look.y < 0 ? Mathf.Abs(verticalVel * fixedDeltaTime) : 1;
                        Vector2 overShoot = (sand is SandBall)
                            ? Vector2.zero
                            : (dist / MaxDist(sand)) * stats.sandOvershoot * new Vector2(frameInput.Look.x, frameInput.Look.y * downVel);

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
                Debug.DrawRay(targetHit.point, Vector2.up * 5, Color.green);

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
        if (!isEntryLaunching) 
            return failedData;

        Vector2 castVector = stats.directSandDashDetectionDistance * FrameDisplacement.normalized + FrameDisplacement;

        float dist = castVector.magnitude;
        Vector2 dir = castVector / dist;

        RaycastHit2D hit = Physics2D.Raycast
            (pos, dir, dist, sharedStats.collisionLayerMask);

        if (hit && hit.transform.TryGetComponent(out ISand sand) && sand.IsBurrowable && sand is BurrowSand && sand != entrySand)
        {
            Debug.DrawLine(pos, hit.point, Color.green, 100);
            return new BurrowMovement.BurrowMovementTransitionData(dir, HeightAdjustedEntryPoint(hit.point, dir), sand);
        }

        RaycastHit2D boxHit = Physics2D.BoxCast
         (pos, col.bounds.size, Vector2Utility.GetVector2Angle(dir), dir, dist, sharedStats.collisionLayerMask);

        if (boxHit && boxHit.transform.TryGetComponent(out sand) && sand.IsBurrowable && sand is BurrowSand && sand != entrySand)
        {
            Debug.DrawLine(pos, boxHit.point, Color.green, 100);
            return new BurrowMovement.BurrowMovementTransitionData(dir, HeightAdjustedEntryPoint(boxHit.point, dir), sand);
        }

        return failedData;
    }

    private IStateSpecificTransitionData TransitionToSandEntry()
    {
        if (isEntryLaunching) return failedData;

        Vector2 dir = Vector2.down;

        RaycastHit2D hit = Physics2D.Raycast(pos + vel * fixedDeltaTime + Vector2.down * HalfHeight, dir, stats.groundedDistance, sharedStats.collisionLayerMask);

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

    private Vector2 HeightAdjustedEntryPoint(Vector2 point, Vector2 dir) => point + HalfHeight * dir;
    #endregion

    #region Collisions
    public void TriggerEnter(IPlayerCollisionInteractor collisionListener)
    {
        if (collisionListener == null) return;

        if (collisionListener is SlowingArea)
        {
            isBeingSlowed = true;
            ApplyMomentum(-stats.slowAreaMomentumLoss);
        }
    }
    public void TriggerExit(IPlayerCollisionInteractor collisionListener)
    {
        if (collisionListener == null) return;
        if (collisionListener is SlowingArea) isBeingSlowed = false;
    }

    #endregion
}