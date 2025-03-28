using UnityEditor.Overlays;
using UnityEngine;
using static TransitionLibrary;

public class BurrowMovement : IState
{
    private readonly BurrowMovementStats stats;
    private readonly MovementStatsHolder sharedStats;
    private readonly PlayerControls controls;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;

    private float PlayerHalfWidth => col.bounds.extents.x;
    private float PlayerHalfHeight => col.bounds.extents.y;

    public BurrowMovement(PlayerControls controls, Rigidbody2D rb, Collider2D col, MovementStatsHolder stats)
    {
        this.controls = controls;
        this.col = col;
        sharedStats = stats;
        this.stats = stats.burrowStats;
        this.rb = rb;
    }


    public void InitializeTransitions(MovementStateMachine controller)
    {
        controller.AddTransition(GetType(), typeof(LandMovement), TransitionToLand);
    }

    public void EnterState(IStateSpecificTransitionData lastStateData) 
    {
        if (lastStateData is BurrowMovementTransitionData transitionData)
        {
            wishDir = moveDir = transitionData.EntryDir;
            rb.position = transitionData.EntryPos;
        }
        ExecuteDash();
    }

    public void ExitState() 
    {
        ResetAllVelocities();
        ExitDash();
        ExitBounce();
        time = 0;
    }


    private Player.Input frameInput;

    private float deltaTime;
    private float time;

    public void Update(Player.Input frameInput)
    {
        time += Time.deltaTime;
        HandleInput(frameInput);
    }

    public void HandleInput(Player.Input frameInput)
    {
        this.frameInput = frameInput;
        if (frameInput.SandDashDown) dashRequested = true;
    }

    public void UpdateMovement()
    {
        deltaTime = Time.deltaTime;

        HandleBounce();
        HandleDash();
        HandleBurrowMovement();
        ApplyMovement();
    }


    private Vector2 wishDir;
    private Vector2 vel;

    private Vector2 FrameDisplacement => (bounceVel + moveVel + dashVel) * deltaTime;
    private Vector2 VelocityDir => vel.normalized;
    private float GetVector2Angle(Vector2 dir) => Mathf.Rad2Deg * Mathf.Atan2(dir.y, dir.x);

    #region Bounce

    private Vector2 bounceVel;
    private Vector2 targetBounceVel;
    private float timeBounced = float.MinValue;
    private float bounceMoveSpeedMult = 1;
    private bool isBouncing;
    private bool bounceInterrupted;

    private void HandleBounce()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            rb.position,
            col.bounds.size,
            GetVector2Angle(vel),
            vel.normalized,
            vel.magnitude * deltaTime,
            sharedStats.terrainLayerMask);

        float timeSinceBounce = time - timeBounced;
        float timePercent = Mathf.Clamp01(timeSinceBounce / stats.bounceDuration);
        if (timePercent == 1 && isBouncing) bounceInterrupted = true;

        if (hit
           && hit.transform.TryGetComponent(out TraversableTerrain _)
           && !isBouncing && timeSinceBounce >= stats.BOUNCE_COOL_DOWN + stats.bounceDuration)
        { ExecuteBounce(hit.normal); }

        if (bounceInterrupted) ExitBounce();
        else if (isBouncing)
        {
            float speedPercent = stats.bounceSpeedCurve.Evaluate(timePercent);
            bounceVel = targetBounceVel * speedPercent;

            float controlPercent = stats.bounceControlCurve.Evaluate(timePercent);
            bounceMoveSpeedMult = stats.bounceMoveSpeedMult * controlPercent;
        }
    }

    private void ExitBounce()
    {
        isBouncing = false;
        bounceInterrupted = false;
        timeBounced = float.MinValue;
        bounceVel = Vector2.zero;
        bounceMoveSpeedMult = 1;
    }

    private void ExecuteBounce(Vector2 hitNormal)
    {
        isBouncing = true;
        dashInterrupted = true;
        timeBounced = time;

        Vector2 reflectedVel = Vector2.Reflect(vel.normalized, hitNormal);
        Vector2 bounceDir = Vector2.Lerp(hitNormal, reflectedVel, stats.bounceNormalBias);

        targetBounceVel = bounceDir * stats.bounceSpeed;
        wishDir = bounceDir;
        moveDir = bounceDir;
    }

    #endregion


    #region Input Movement

    private Vector2 moveDir = Vector2.right;
    private Vector2 moveVel;
    private void HandleBurrowMovement()
    {
        if (frameInput.Move != Vector2.zero) wishDir = frameInput.Move;

        float angle = Mathf.MoveTowardsAngle(
            GetVector2Angle(moveDir),
            GetVector2Angle(wishDir),
            stats.rotationSpeed);

        moveDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

        float accel = stats.acceleration;
        Vector2 moveVelChange = accel * deltaTime * moveDir;
        moveVel = Vector2.ClampMagnitude(bounceMoveSpeedMult * (moveVel + moveVelChange), stats.maxSpeed);
    }

    #endregion


    #region Dash

    private bool dashRequested;
    private bool isDashing;
    private float dashControlMult = 1;
    private float timeDashed = float.MinValue;
    private bool dashInterrupted;
    private Vector2 dashVel;
    private Vector2 targetDashVel;

    private void HandleDash()
    {
        float timePercent = Mathf.Clamp01((time - timeDashed) / stats.dashDuration);
        if ((timePercent == 1 && isDashing) || isBouncing)
            dashInterrupted = true;

        bool canDash = !isDashing && !dashInterrupted;
        if (dashRequested && canDash) ExecuteDash();
        dashRequested = false;

        if (dashInterrupted) ExitDash();
        else if (isDashing)
        {
            float speedPercent = stats.dashSpeedCurve.Evaluate(timePercent);
            dashVel = targetDashVel * speedPercent;

            float controlPercent = stats.dashControlCurve.Evaluate(timePercent);
            dashControlMult = stats.dashControlMult * controlPercent;
        }
    }

    private void ExitDash()
    {
        dashControlMult = 1;
        dashVel = Vector2.zero;
        dashControlMult = 1;
        dashInterrupted = false;
        isDashing = false;
    }

    private void ExecuteDash()
    {
        timeDashed = time;
        dashInterrupted = false;
        isDashing = true;

        targetDashVel = stats.dashSpeed * moveDir;
    }

    #endregion


    #region Transitions

    public class BurrowMovementTransitionData : SuccesfulTransitionData
    {
        public Vector2 EntryDir { get; }
        public Vector2 EntryPos { get; }

        public BurrowMovementTransitionData(Vector2 entryDir, Vector2 entryPos)
        {
            EntryDir = entryDir;
            EntryPos = entryPos;
        }
    }

    private IStateSpecificTransitionData TransitionToLand()
    {
        //Ray is cast out to in, to prevent sand exit if there is terrain beyond sand
        Vector2 dir = VelocityDir;
        Vector2 origin = rb.position + FrameDisplacement + dir * PlayerHalfHeight;
        float distance = FrameDisplacement.magnitude;

        ISand sand = null;
        RaycastHit2D hit = Physics2D.Raycast(origin, -dir, distance, sharedStats.collisionLayerMask);

        bool canExit = hit
            && hit.transform.TryGetComponent(out sand)
            && sand is BurrowSand;

        Debug.DrawLine(origin, origin - dir * distance, canExit ? Color.green : Color.red);

        if (canExit)
        {
            sand.OnSandBurrowExit(vel, rb.position);

            rb.position = hit.point + dir * PlayerHalfHeight;
            return new LandMovement.LandMovementTransition(dir, isDashing);
        }

        return failedData;
    }


    #endregion

    private void ResetAllVelocities()
    {
        moveVel = Vector2.zero;
        bounceVel = Vector2.zero;
        dashVel = Vector2.zero;
        vel = Vector2.zero;
    }

    private void ApplyMovement()
    {
        vel = moveVel + bounceVel + dashVel;
        rb.linearVelocity = vel;

        col.transform.eulerAngles = new Vector3(col.transform.eulerAngles.x, col.transform.eulerAngles.y, GetVector2Angle(moveDir) + 90);
    }
}
