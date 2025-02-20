using System;
using UnityEngine;

public class LandMovement : BaseMovementState
{
    private PlayerLandControllerStats stats;

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
            Move = controls.PlayerLand.Move.ReadValue<float>()
        };

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

    private float SlipMultiplier => wasOnSlipperyGround ? 1 / stats.slipMultiplier : 1;

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

    private bool NormalInWallRange(Vector2 normal) => Mathf.Abs(normal.y) >= 0 && Mathf.Abs(normal.y) <= stats.wallNormalRange;
    private void HandleWallDetection(RaycastHit2D topRightHit, RaycastHit2D middleRightHit, RaycastHit2D bottomRightHit, RaycastHit2D topLeftHit, RaycastHit2D middleLeftHit, RaycastHit2D bottomLeftHit)
    {
        Vector2 averageWallNormal;
        bool newIsOnWall = false;
        bool canGrabWall = !isGrounded && ((lastSurfaceType == LastSurfaceType.Ground) || (lastSurfaceType == LastSurfaceType.Wall)) && velocity.y < stats.maxYVelocityForWallGrab;

        HandleWallGrab(topRightHit, middleRightHit, 1);
        HandleWallGrab(topLeftHit, middleLeftHit, -1);

        void HandleWallGrab(RaycastHit2D topHit, RaycastHit2D middleHit, int dir)
        {
            averageWallNormal = (topHit.normal + middleHit.normal).normalized;
            bool wallGrabbable = NormalInWallRange(averageWallNormal) && ((middleHit && middleHit.transform.CompareTag(stats.wallTag)) || (topHit && topHit.transform.CompareTag(stats.wallTag)));

            if (wallGrabbable && canGrabWall)
            {
                if (topHit.transform.TryGetComponent(out Terrain topTerrain)) wasOnSlipperyGround = topTerrain.isSlippery;
                else if (middleLeftHit.transform.TryGetComponent(out Terrain middleTerrain)) wasOnSlipperyGround = middleTerrain.isSlippery;
                else wasOnSlipperyGround = false;

                newIsOnWall = true;
                lastSurfaceType = LastSurfaceType.Wall;

                playerRB.linearVelocityX = 0;
                jumpAndGravVelocity.x = 0;

                wallNormal = averageWallNormal;
            }
        }

        if (newIsOnWall ^ isOnWall) OnChangeWall(newIsOnWall);

        //Ledge grab
        if (((bottomRightHit && !topRightHit && !middleRightHit) || (bottomLeftHit && !topLeftHit && !middleLeftHit)) && !isGrounded)
        {
            RaycastHit2D ledgeGrabHit = bottomRightHit ? bottomRightHit : bottomLeftHit;

            if (!ledgeGrabHit.transform.CompareTag(stats.wallTag))
            {
                int dir = bottomRightHit ? 1 : -1;

                Vector2 ledgeNormal = ledgeGrabHit.normal;
                float dist = Mathf.Abs(ledgeGrabHit.point.x - (position.x + dir * PlayerHalfWidth));

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
        }
        else
        {
            leftSurfaceTime = time;
            if (!isJumping) shouldApplyGravityFallof = true;
        }
    }

    #endregion

    #region Ground
    public bool isGrounded { get; private set; }
    private bool wasOnSlipperyGround;

    private void HandleGround(RaycastHit2D bottomLeftHit, RaycastHit2D bottomMiddleHit, RaycastHit2D bottomRightHit)
    {
        bool newGrounded = false;
        Vector2 averageGroundNormal = (bottomLeftHit.normal + bottomRightHit.normal + bottomMiddleHit.normal).normalized;


        if (bottomLeftHit || bottomRightHit || bottomMiddleHit)
        {
            if (bottomLeftHit.transform.TryGetComponent(out Terrain leftTerrain)) wasOnSlipperyGround = leftTerrain.isSlippery;
            else if (bottomMiddleHit.transform.TryGetComponent(out Terrain middleTerrain)) wasOnSlipperyGround = middleTerrain.isSlippery;
            else if (bottomLeftHit.transform.TryGetComponent(out Terrain rightTerrain)) wasOnSlipperyGround = rightTerrain.isSlippery;
            else wasOnSlipperyGround = false;

            if (averageGroundNormal.y > stats.minGroundNormal)
            {
                newGrounded = true;
                groundNormal = averageGroundNormal;
                lastSurfaceType = LastSurfaceType.Ground;
            }
        }

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
            if (!isJumping) shouldApplyGravityFallof = true;
        }
    }

    #endregion

    #region Momentum

    private void HandleMomentum()
    {
        if ((horizontalVelocity.magnitude > stats.momentumGainThreshold || jumpAndGravVelocity.magnitude > stats.momentumGainThreshold))
        { momentum += stats.momentumGainSpeed * fixedDeltaTime; }
        else { momentum -= stats.momentumLossSpeed * fixedDeltaTime; }
        momentum = Mathf.Clamp01(momentum);
    }

    private void ApplyMomentum(float amount)
    {
        momentum += amount;
        momentum = Mathf.Clamp01(momentum);
    }

    #endregion

    #region HorizontalMovement

    private float targetSpeed;
    private float acceleration;
    public float momentum;

    public float turningDot => 1 - (playerRB.linearVelocity.normalized.x + frameInput.Move);

    private void HandleHorizontalMovement()
    {
        float moveInput = frameInput.Move;
        float absMoveInput = Mathf.Abs(moveInput);

        float turningMultiplier = Mathf.Lerp(1, stats.turningAccelerationMultiplier *  SlipMultiplier, turningDot);
        float apexBonus = moveInput * stats.apexSpeedIncrease * ApexPoint;

        acceleration = absMoveInput > 0 
            ? Mathf.Lerp(stats.minAcceleration, stats.maxAcceleration, momentum) * turningMultiplier * SlipMultiplier
            : Mathf.Lerp(stats.minDeceleration, stats.maxDeceleration, momentum) * SlipMultiplier;

        targetSpeed = Mathf.Lerp(stats.minLandSpeed, stats.maxLandSpeed, momentum) * moveInput * 1/SlipMultiplier;

        if (IsInAir)
        {
            targetSpeed *= stats.airSpeedMultiplier;
            targetSpeed += apexBonus;

            acceleration *= stats.airAccelerationMultiplier;
        }

        float speed = Mathf.MoveTowards(horizontalVelocity.x, targetSpeed, acceleration * fixedDeltaTime);

        if (lastSurfaceType == LastSurfaceType.Wall) speed = Mathf.Lerp(0, speed, (time - leftSurfaceTime) / stats.timeToFullSpeedFromWall);

        horizontalVelocity = speed * groundHorizontal;
        jumpAndGravVelocity.x = Mathf.MoveTowards(jumpAndGravVelocity.x, 0, acceleration * fixedDeltaTime);
    }

    #endregion

    #region Jump

    private enum LastSurfaceType
    {
        Wall,
        Ground
    }

    private LastSurfaceType lastSurfaceType = LastSurfaceType.Ground;

    private bool shouldApplyGravityFallof;
    private bool coyoteUsable;
    private bool bufferedJumpUsable;
    private float leftSurfaceTime = float.MinValue;
    private bool isJumping;
    private bool jumpRequested;
    private float timeJumpRequested = float.MinValue;

    public float ApexPoint
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

        if (lastSurfaceType == LastSurfaceType.Wall)
        {
            WallJumped?.Invoke();

            jumpAndGravVelocity = stats.wallJumpForce * Vector2.LerpUnclamped(wallNormal, Vector2.up, stats.wallJumpUpBias);
            ApplyMomentum(stats.wallJumpMomentumIncrease);
        }
        if (lastSurfaceType == LastSurfaceType.Ground)
        {
            Jumped?.Invoke();

            jumpAndGravVelocity = stats.jumpForce * Vector2.LerpUnclamped(groundNormal, Vector2.up, stats.jumpUpBias);
            ApplyMomentum(stats.jumpMomentumIncrease);
        }
    }

    #endregion

    #region Gravity

    public bool IsInAir => !isGrounded && !isOnWall;

    private void HandleGravity()
    {
        if (isGrounded && !isJumping) jumpAndGravVelocity = Vector2.zero;
        if (!isGrounded)
        {
            float apexAntiGravity = Mathf.Lerp(0, -stats.apexAntigravity, ApexPoint);
            float downwardAcceleration;


            if (isOnWall && !isJumping)
            {
                downwardAcceleration = stats.onWallGravity * SlipMultiplier;
            }
            else
            {
                downwardAcceleration = stats.gravity;
                downwardAcceleration += apexAntiGravity;

                if ((shouldApplyGravityFallof && jumpAndGravVelocity.y > 0 || jumpAndGravVelocity.y < stats.jumpVelocityFallof))
                {
                    downwardAcceleration *= stats.gravAfterFalloffMultiplier;
                }
            }

            jumpAndGravVelocity.y = Mathf.MoveTowards(jumpAndGravVelocity.y, -stats.maxDownVelocity, downwardAcceleration * Time.fixedDeltaTime);
        }
    }

    #endregion

    private void ApplyMovement()
    {
        if (isGrounded && !isJumping) jumpAndGravVelocity.y = stats.groundingPush;
        velocity = jumpAndGravVelocity + horizontalVelocity;

        playerRB.position = position;
        playerRB.linearVelocity = velocity;
    }
}
