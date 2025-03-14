using System;
using UnityEngine;
using static TransitionLibrary;

public class LandMovement : IState
{
    private readonly MovementStatsHolder sharedStats;
    private readonly LandMovementStats stats;
    private readonly PlayerControls controls;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;

    private float PlayerHalfWidth => col.bounds.extents.x;
    private float PlayerHalfHeight => col.bounds.extents.y;

    public LandMovement(PlayerControls controls, Rigidbody2D rb, Collider2D col, MovementStatsHolder stats) 
    {
        this.controls = controls;
        this.col = col;
        sharedStats = stats;
        this.stats = stats.landStats;
        this.rb = rb;
    }

    public void InitializeTransitions(MovementStateMachine controller)
    {
        controller.AddTransition(GetType(), typeof(BurrowMovement), TransitionToSandEntry);
        controller.AddTransition(GetType(), typeof(InterStateDashMovement), TransitionToInterStateDash);
    }

    public void EnterState(IStateSpecificTransitionData lastStateData)
    {
        Debug.Log("entered");

        if (lastStateData is LandMovementTransition transitionData)
        {
            firstFrameOfTransition = true;
            ExecuteEntryLaunch(transitionData.WasDashing, transitionData.EntryDir);
            col.transform.rotation = Quaternion.Euler(transitionData.EntryDir);
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
    }

    private Player.Input frameInput;
    private float HorizontalInput => frameInput.HorizontalMove;
    private bool firstFrameOfTransition;
    private float fixedDeltaTime;
    private float time;

    public void Update(Player.Input frameInput)
    {
        time += Time.deltaTime;
        HandleInput(frameInput);
    }
    public void HandleInput(Player.Input frameInput)
    {
         this.frameInput = frameInput;

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


    public void UpdateMovement()
    {
        fixedDeltaTime = Time.fixedDeltaTime;
        position = rb.position;
        vel = rb.linearVelocity;

        HandleCollisionInteractions();
        HandleJump();
        HandleGravity();
        HandleDash();
        HandleEntryLaunch();
        HandleHorizontalMovement();
        HandleMomentum();
        HandleVelocity();
        ApplyMovement();

        firstFrameOfTransition = false;
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

    private Vector2 entryLaunchVel;
    private Vector2 targetLaunchVel;

    private float entryLaunchSpeedMult;
    private float entryLaunchGravMult;

    private void HandleEntryLaunch()
    {
        float timePercent = Mathf.Clamp01(time / stats.launchDuration);

        if (isBeingSlowed
         || (timePercent == 1 && isEntryLaunching) 
         || isDashing || hitCeilingThisFrame) 
            entryLaunchInterrupted = true;

        if (entryLaunchInterrupted) ExitEntryLaunch();
        else if (isEntryLaunching)
        {
            entryLaunchSpeedMult = stats.launchControlCurve.Evaluate(timePercent);
            entryLaunchGravMult = stats.launchGravCurve.Evaluate(timePercent);

            entryLaunchVel = targetLaunchVel * stats.launchSpeedCurve.Evaluate(timePercent);

            //Applies friction if player is trying to oppose dash velocity for greater control
            float xDecel = VelocityOpposingMovementStrength(entryLaunchVel) * stats.launchOpposingMovementFriction * Mathf.Abs(entryLaunchVel.x);
            entryLaunchVel.x = Mathf.MoveTowards(entryLaunchVel.x, 0, fixedDeltaTime * xDecel);
        }
    }

    private void ExecuteEntryLaunch(bool strongLaunch, Vector2 entryLaunchDir)
    {
        isEntryLaunching = true;
        targetLaunchVel = (strongLaunch ? stats.launchVel : stats.weakLaunchVel) * entryLaunchDir;
        Debug.Log("Executed entry launch with velocity " + targetLaunchVel);
    }
    private void ResetEntryLaunch()
    {
        entryLaunchInterrupted = false;
        isEntryLaunching = false;
        entryLaunchVel = Vector2.zero;
        entryLaunchSpeedMult = 1;
        entryLaunchGravMult = 1;
    }

    private void ExitEntryLaunch()
    {
        entryLaunchInterrupted = false;
        isEntryLaunching = false;
        entryLaunchVel = Vector2.zero;
        entryLaunchSpeedMult = 1;
        entryLaunchGravMult = 1;
    }

    #endregion

    #region TerrainBehaviour

    TraversableTerrain terrainOn;
    private void HandleTerrainChange(TraversableTerrain newTerrain)
    {
        if (newTerrain == null || newTerrain == terrainOn) return;
        terrainOn = newTerrain;

        if (firstFrameOfTransition) return;

        wasOnSlipperyGround = newTerrain is SlipperyGround;
        if (newTerrain is not UnstableSand) EventsManager.Instance.InvokePlayerLandOnStableGround();

        newTerrain.OnPlayerEnterTerrain(this);
    }

    /// <summary>
    /// Returns true if hit transform has component TraversableTerrain and changes terrain refrence accordingly
    /// </summary>
    private bool TryGetHitTerrain(RaycastHit2D hit, out TraversableTerrain terrain)
    {
        terrain = null;
        return hit ? hit.transform.TryGetComponent(out terrain) : false;
    }

    /// <summary>
    /// Between 0 and 1, how strongly the desired input opposes with the current velocity
    /// </summary>
    public float VelocityOpposingMovementStrength(Vector2 vel) => Mathf.Clamp01(1 - Mathf.Abs(vel.normalized.x + FacingDirection));

    private void HandleCollisionInteractions()
    {
        Vector2 rightOrigin = Vector2.right * (PlayerHalfWidth - stats.skinWidth);
        Vector2 downOrigin = Vector3.down * (PlayerHalfHeight - stats.skinWidth);
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

        if (((!topRightHit && topLeftHit) || (!topLeftHit && topRightHit)) && averageCeilNormal.y < stats.maxCeilNormal && topMiddleHit)
        {
            if (topLeftHit) ceilingNudgeDir = (topLeftHit.point - position).normalized;
            if (topRightHit) ceilingNudgeDir = (topRightHit.point - position).normalized;

            hitCeilingThisFrame = true;
            position += ceilingNudgeDir * frameInput.Move;
        }
        else if (topLeftHit || topMiddleHit || topRightHit)
        {
            verticalVel = Mathf.Min(averageCeilNormal.y * stats.ceilingHitPush, verticalVel);
            shouldApplyGravFallof = true;
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
    private bool HitGrabbableWall(RaycastHit2D hit) => hit && hit.transform.GetComponent<WallGrabbableTerrain>() != null;

    private void HandleWallDetection(RaycastHit2D topRightHit, RaycastHit2D middleRightHit, RaycastHit2D bottomRightHit, RaycastHit2D topLeftHit, RaycastHit2D middleLeftHit, RaycastHit2D bottomLeftHit)
    {
        TraversableTerrain newTerrain = null;
        Vector2 newWallNormal;
        bool newIsOnWall = false;
        bool canGrabWall = !isGrounded && vel.y < stats.maxYVelForWallGrab;
        isPushingWall = false;

        HandleWallGrab(topRightHit, middleRightHit);
        HandleWallGrab(topLeftHit, middleLeftHit);

        void HandleWallGrab(RaycastHit2D topHit, RaycastHit2D middleHit)
        {
            RaycastHit2D hit = new();
            if (middleHit) hit = middleHit;
            else if (topHit) hit = topHit;
            else return;

            newWallNormal = hit.normal;
            bool normalInWallRange = NormalInWallRange(newWallNormal);

            if (normalInWallRange)
            {
                int dir = (int)Mathf.Sign(hit.point.x - rb.position.x);
                if (Mathf.Sign(horizontalVel) == dir || Mathf.Sign(dashVel.x) == dir) isPushingWall = true;

                if (HitGrabbableWall(hit) && canGrabWall)
                {
                    newIsOnWall = true;

                    TryGetHitTerrain(hit, out newTerrain);
                    lastSurfaceType = LastSurfaceType.Wall;

                    wallNormal = newWallNormal;
                }
            }
        }

        HandleTerrainChange(newTerrain);
        if (newIsOnWall ^ isOnWall) OnChangeWall(newIsOnWall);

        HandleLedgeGrab();
        void HandleLedgeGrab()
        {
            if (((bottomRightHit && !topRightHit && !middleRightHit) || (bottomLeftHit && !topLeftHit && !middleLeftHit)) && !isGrounded)
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
        isOnWall = newWall;
        if (firstFrameOfTransition) return;


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
            timeLeftSurfce = time;
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
            if (newGroundNormal.y > stats.minGroundNormal && TryGetHitTerrain(hit, out newTerrain))
            {
                newGrounded = true;
                lastSurfaceType = LastSurfaceType.Ground;
            }
        }
        HandleTerrainChange(newTerrain);
        if (isGrounded ^ newGrounded) OnChangeGrounded(newGrounded);
    }


    private void OnChangeGrounded(bool newGrounded)
    {
        isGrounded = newGrounded;
        if (firstFrameOfTransition) return;

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
            timeLeftSurfce = time;

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
    private float acceleration;

    private void HandleHorizontalMovement()
    {
        float moveInput = HorizontalInput;
        if (isPushingWall) 
            ResetHorizontalVel();

        float turningMult = Mathf.Lerp(1, stats.turningAccelerationMultiplier * SlipMultiplier, VelocityOpposingMovementStrength(vel));
        float apexBonus = moveInput * stats.apexSpeedIncrease * ApexProximity;

        //used for slower exit from wall
        float wallExitMult = lastSurfaceType == LastSurfaceType.Wall
            ? Mathf.Clamp01((time - timeLeftSurfce) / stats.timeToFullSpeedFromWall)
            : 1;

        acceleration = Mathf.Abs(moveInput) > 0
            ? Mathf.Lerp(stats.minAcceleration, stats.maxAcceleration, momentum) * turningMult   //acceleration
            : Mathf.Lerp(stats.minDeceleration, stats.maxDeceleration, momentum);                //deceleration

        targetSpeed = moveInput * 1 / SlipMultiplier * Mathf.Lerp(stats.minLandSpeed, stats.maxLandSpeed, momentum);

        if (IsInAir)
        {
            targetSpeed = targetSpeed * stats.airSpeedMultiplier + wallJumpHorizontalVel + apexBonus;
            acceleration *= stats.airAccelerationMultiplier;
        }
        else acceleration *= SlipMultiplier;   //For faster aceleration in air when slippery

        float desiredSpeed = Mathf.MoveTowards(horizontalVel, targetSpeed, acceleration * fixedDeltaTime);
        horizontalVel = desiredSpeed * wallExitMult + wallJumpHorizontalVel;

        wallJumpHorizontalVel = Mathf.MoveTowards(wallJumpHorizontalVel, 0, Mathf.Abs(horizontalVel) * stats.additionalHorizontalMovementDeceleration * fixedDeltaTime);
    }

    private void ResetHorizontalVel()
    {
        wallJumpHorizontalVel = 0;
        horizontalVel = 0;
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
    private float timeLeftSurfce = float.MinValue;
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
    private bool CanUseCoyote => coyoteUsable && !isGrounded && !isOnWall && time < timeLeftSurfce + stats.coyoteTime;

    private void HandleJump()
    {
        if (isDashing) return;

        //If jump ended early, apply gravity fallof
        if (!isGrounded && !frameInput.JumpHeld && vel.y > 0 && isJumping) shouldApplyGravFallof = true;

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
        Debug.Log(wallNormal);
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

    private void ResetJump()
    {
        timeLeftSurfce = float.MinValue;
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
        float gravLaunchMult = 
            isDashing ? dashGravMult 
            : isEntryLaunching ? entryLaunchGravMult 
            : 1;

        if (isGrounded) return;
        if (isOnWall) currentGravity = stats.onWallGravity;
        else
        {
            float apexAntiGravity = Mathf.Lerp(0, -stats.apexAntigravity, ApexProximity);
            float gravFallof = shouldApplyGravFallof && verticalVel > 0 || vel.y < stats.jumpVelocityFallof
                ? stats.gravAfterFalloffMultiplier
                : 1;

            currentGravity = (stats.gravity + apexAntiGravity) * gravFallof;
        }

        verticalVel = Mathf.MoveTowards(verticalVel, -stats.maxDownVel, currentGravity * fixedDeltaTime * gravLaunchMult);
    }
    #endregion

    #region Dash

    private bool dashRequested;
    private bool isDashing;

    private float dashSpeedMult = 1;
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

        bool canDash = !isDashing && !dashInterrupted;
        if (dashRequested && canDash) ExecuteDash();
        dashRequested = false;

        if (dashInterrupted) ExitDash();
        else if (isDashing) 
        {
            dashSpeedMult = stats.dashHorizontalVelMult.Evaluate(timePercent);
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
        timeDashed = float.MinValue;
    }

    private void ExitDash()
    {
        dashInterrupted = false;
        isDashing = false;
        dashVel = Vector2.zero;
        dashSpeedMult = 1;
        dashGravMult = 1;
    }

    private void ExecuteDash()
    {
        isDashing = true;
        timeDashed = time;

        shouldApplyGravFallof = true; //To prevent floaty dash if jump and dash are executed at the same time
        verticalVel = 0;

        targetDashVel = frameInput.JumpHeld ? stats.diagonalDashVel : stats.horizontalDashVel;
        targetDashVel.x *= FacingDirection;

        //Prevents horizontal velocity opposite dash velocity when doing sudden turn dashes
        horizontalVel = FacingDirection * Mathf.Max(horizontalVel * FacingDirection, 0);
    }


    #endregion

    #region Velocty

    private void HandleVelocity()
    {
        float horizontalLaunchMult =
            isDashing ? dashSpeedMult
            : isEntryLaunching ? entryLaunchSpeedMult
            : 1;

        Vector2 launchVel = isDashing ? dashVel 
            : isEntryLaunching ? entryLaunchVel
            : Vector2.zero;

        Vector2 drag = SlowAreaDrag;

        horizontalVel = Mathf.MoveTowards(horizontalVel, 0, drag.x * fixedDeltaTime * Mathf.Abs(horizontalVel));
        verticalVel = Mathf.MoveTowards(verticalVel, 0, drag.y * fixedDeltaTime * Mathf.Abs(verticalVel));

        if (isGrounded && !isJumping) verticalVel = stats.groundingPush;

        vel = Vector2.up * verticalVel 
            + Vector2.right * (horizontalVel * horizontalLaunchMult) 
            + launchVel;

        nonZeroVelocityDirection = new Vector2(
            vel.x != 0 ? vel.x : nonZeroVelocityDirection.x,
            vel.y != 0 ? vel.y : nonZeroVelocityDirection.y)
            .normalized;
    }

    private void ResetAllVelocities()
    {
        horizontalVel = 0;
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

    public class LandMovementTransition : AnyTransitionData
    {
        public bool WasDashing { get; }
        public Vector2 EntryDir { get; }

        public LandMovementTransition(Vector2 entryDir, bool wasDashing) 
        {
            EntryDir = entryDir;
            WasDashing = wasDashing;
        }
    }

    private IStateSpecificTransitionData TransitionToInterStateDash()
    {
        Collider2D closestSand = Physics2D.OverlapCircle(position, stats.sandDetectionDistance, sharedStats.sandLayerMask);

        if (closestSand != null && closestSand.transform.TryGetComponent(out HardSand _))
        {
            Vector2 closestPoint = closestSand.ClosestPoint(position);
            Vector2 diff = (closestPoint - position);
            Vector2 dir = diff.normalized;
            float dist = diff.magnitude;

            RaycastHit2D hit = Physics2D.Raycast(
                position, dir,
                dist,
                sharedStats.terrainLayerMask);

            if (hit) Debug.DrawLine(position, hit.point, Color.red);
            else
            {
                Debug.DrawLine(position, closestPoint, Color.green);

                if (frameInput.DashDown)
                {
                    Vector2 entryPoint = closestPoint + dir * (PlayerHalfHeight + 0.5f);
                    return new InterStateDashMovement.InterStateDashData(entryPoint);
                }
            }
        }

        return failedData;
    }

    private IStateSpecificTransitionData TransitionToSandEntry()
    {
        Vector2 dir = Vector2.down;

        RaycastHit2D sandHit = Physics2D.BoxCast(position, col.bounds.size, 0, 
            dir, stats.groundedDistance, sharedStats.collisionLayerMask);

        if(sandHit && sandHit.transform.TryGetComponent(out HardSand _))
        {
            if (sandHit && frameInput.DashDown)
            {
                Vector2 entryPoint = sandHit.point + dir * (PlayerHalfHeight + 0.5f);
                return new BurrowMovement.BurrowMovementTransitionData(dir, entryPoint);
            }
        }

        return failedData;
    }

    #endregion

    #region Collisions
    public void TriggerEnter(IPlayerCollisionInteractor collisionInteractor)
    {
        if (collisionInteractor == null) return;
        if (collisionInteractor is SlowingArea)
        {
            isBeingSlowed = true;
            dashInterrupted = true;
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