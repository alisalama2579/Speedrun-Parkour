using System;
using UnityEngine;
using static TransitionLibrary;

public class BurrowMovement : IMovementState
{
    private readonly BurrowMovementStats stats;
    private readonly MovementStatsHolder sharedStats;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;
    private readonly PlayerMovementProperties properties;
    private float PlayerHalfHeight => col.bounds.extents.y;
    private Transform transform;

    public BurrowMovement(MovementInitData movementData)
    {
        col = movementData.Col;
        sharedStats = movementData.Stats;
        stats = movementData.Stats.burrowStats;
        rb = movementData.RB;
        transform = movementData.Transform;
        properties = movementData.Properties;
    }


    public void InitializeTransitions(PlayerStateMachine controller)
    {
        controller.AddTransition(GetType(), typeof(LandMovement), TransitionToLand);
    }

    public event Action OnPlayerEnterBurrow;

    public void EnterState(IStateSpecificTransitionData lastStateData) 
    {
        if (lastStateData is BurrowMovementTransitionData transitionData)
        {
            wishDir = transitionData.EntryDir;
            moveDir = transitionData.EntryDir;
            rb.position = transitionData.EntryPos;

            transitionData.EntrySand.OnSandEnter(wishDir, rb.position);

            entrySand = transitionData.EntrySand;
            EventsHolder.PlayerEvents.OnPlayerEnterSand?.Invoke(entrySand);

            dashControlMult = 2;
            ExecuteDash(transitionData.EnteredDirectly);
        }

        OnPlayerEnterBurrow?.Invoke();
    }

    public event Action OnPlayerExitBurrow;
    public void ExitState() 
    {
        ResetAllVelocities();
        ExitDash();
        ExitBounce();
        frame = 0;
        time = 0;
        frameInput = new();
        timeDashRequested = float.MinValue;

        EventsHolder.PlayerEvents.OnPlayerExitSand?.Invoke(entrySand);
        OnPlayerExitBurrow?.Invoke();
    }


    private MovementInput frameInput;

    private float deltaTime;
    private float time;

    private ISand entrySand;

    public void Update(MovementInput frameInput)
    {
        time += Time.deltaTime;
        HandleInput(frameInput);
    }

    public void HandleInput(MovementInput frameInput)
    {
        this.frameInput = frameInput;
        if (frameInput.SandDashDown)
        {
            dashRequested = true;
            timeDashRequested = time;
        }
    }

    private int frame;
    public void FixedUpdate()
    {
        frame++;
        deltaTime = Time.deltaTime;

        HandleBounce();
        HandleDash();
        HandleBurrowMovement();
        ApplyMovement();
    }


    private Vector2 wishDir;
    private Vector2 vel;

    private Vector2 FrameDisplacement => (moveVel + dashVel) * deltaTime;
    private Vector2 VelDir => vel.normalized;
    public Vector2 MoveDir => moveDir;
    public bool IsBurrowDashing => isDashing;


    #region Bounce

    public event Action<Vector2> OnPlayerBounce;

    private float bounceSpeed;
    private float targetBounceSpeed;
    private float timeBounced = float.MinValue;
    private float bounceControlMult = 1;
    private bool isBouncing;
    private bool bounceInterrupted;

    private void HandleBounce()
    {
        float dist = vel.magnitude * deltaTime;
        RaycastHit2D hit = new();

        if(dist > 0 && frame % stats.bounceCheckFrequency == 0)
        {
            Vector2 dir = vel / dist;

            hit = Physics2D.BoxCast(
            rb.position,
            col.bounds.size,
            Vector2Utility.GetUnityVector2Angle(vel),
            dir,
            dist,
            sharedStats.terrainLayerMask);
        }

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
            bounceSpeed = targetBounceSpeed * speedPercent;

            float controlPercent = stats.bounceControlCurve.Evaluate(timePercent);
            bounceControlMult = stats.bounceMoveSpeedMult * controlPercent;
        }
    }

    private void ExitBounce()
    {
        isBouncing = false;
        bounceInterrupted = false;
        timeBounced = float.MinValue;
        bounceSpeed = 0;
        bounceControlMult = 1;
    }

    private void ExecuteBounce(Vector2 hitNormal)
    {
        isBouncing = true;
        dashInterrupted = true;
        timeBounced = time;

        Vector2 reflectedVel = Vector2.Reflect(vel.normalized, hitNormal);
        float normalReflectDot = Mathf.Clamp01(Vector2.Dot(reflectedVel, hitNormal));

        Debug.DrawRay(rb.position, reflectedVel * 10, Color.black, 10);

        targetBounceSpeed = Mathf.Lerp(stats.bounceSpeed, stats.bounceSpeed * 0.5f, normalReflectDot);
        moveVel = reflectedVel * targetBounceSpeed;
        wishDir = reflectedVel;
        moveDir = reflectedVel;

        OnPlayerBounce?.Invoke(reflectedVel * targetBounceSpeed);
    }
    #endregion


    #region Input Movement

    private Vector2 moveDir = Vector2.right;
    private Vector2 moveVel;
    private void HandleBurrowMovement()
    {
        if (frameInput.Look != Vector2.zero) wishDir = frameInput.Look;

        float angle = Mathf.MoveTowardsAngle(
            Vector2Utility.GetUnityVector2Angle(moveDir),
            Vector2Utility.GetUnityVector2Angle(wishDir),
            stats.rotationSpeed * bounceControlMult * dashControlMult);

        moveDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

        float accel = stats.acceleration;
        Vector2 moveVelChange = accel * deltaTime * moveDir;
        moveVel = Vector2.ClampMagnitude((moveVel + moveVelChange), isBouncing ? bounceSpeed : stats.maxSpeed);
    }

    #endregion


    #region Dash

    private bool dashRequested;
    private float timeDashRequested;
    private bool isDashing;
    private float timeDashed = float.MinValue;
    private float dashDuration;
    private bool dashInterrupted;
    private float dashControlMult;
    private float entireDashControlMult;
    private Vector2 dashVel;
    private Vector2 targetDashVel;

    private bool HasBufferedDash => time <= timeDashRequested + stats.dashBuffer;

    public event Action OnBurrowDash;
    private void HandleDash()
    {
        float timePercent = Mathf.Clamp01((time - timeDashed) / dashDuration);
        if ((timePercent >= 1 && isDashing) || isBouncing)
            dashInterrupted = true;

        bool canChainDash = dashRequested && timePercent >= stats.progressToDashChain && !dashInterrupted;
        bool canBufferedDash = HasBufferedDash && !isDashing && !dashInterrupted;
        if (canChainDash || canBufferedDash) ExecuteDash(false);
        dashRequested = false;

        if (dashInterrupted) ExitDash();
        else if (isDashing)
        {
            float speedPercent = stats.dashSpeedCurve.Evaluate(timePercent);
            dashControlMult = stats.dashControlCurve.Evaluate(timePercent) * entireDashControlMult;
            dashVel = targetDashVel * speedPercent;
        }
    }

    private void ExitDash()
    {
        dashControlMult = 1;
        entireDashControlMult = 1;
        dashVel = Vector2.zero;
        dashInterrupted = false;
        isDashing = false;
        timeDashed = float.MinValue;
    }

    private void ExecuteDash(bool directEntry)
    {
        OnBurrowDash?.Invoke();

        timeDashed = time;
        dashInterrupted = false;
        isDashing = true;

        float dashSpeed = directEntry ? stats.dashSpeed * stats.directEntryMultiplier : stats.dashSpeed;
        entireDashControlMult = directEntry ? stats.directDashControlMult : stats.dashControlMult;

        targetDashVel = dashSpeed* moveDir;
        dashDuration = stats.dashDuration;
    }

    //private void ExecuteDash(float speed, float duration)
    //{
    //    OnBurrowDash?.Invoke();

    //    timeDashed = time;
    //    dashInterrupted = false;
    //    isDashing = true;

    //    targetDashVel = speed * moveDir;
    //    dashDuration = duration;
    //}

    #endregion


    #region Transitions

    public class BurrowMovementTransitionData : SuccesfulTransitionData
    {
        public Vector2 EntryDir { get; }
        public Vector2 EntryPos { get; }
        public ISand EntrySand { get; }
        public bool EnteredDirectly { get; }

        public BurrowMovementTransitionData(Vector2 entryDir, Vector2 entryPos, ISand entrySand, bool enteredDirectly)
        {
            EntryDir = entryDir;
            EntryPos = entryPos;
            EntrySand = entrySand;
            EnteredDirectly = enteredDirectly;
        }
    }

    private IStateSpecificTransitionData TransitionToLand()
    {
        const float INSIDE_SAND_ERROR_MARGIN = 0.1f;

        bool cachedStartInCol = Physics2D.queriesStartInColliders;
        bool cachedHitTriggers = Physics2D.queriesHitTriggers;
        Physics2D.queriesStartInColliders = true;
        Physics2D.queriesHitTriggers = true;

        //Ray is cast out to in, to prevent sand exit if there is terrain beyond sand
        Vector2 dir = VelDir;

        Vector2 origin = rb.position + dir * PlayerHalfHeight;
        Vector2 boxBounds = new Vector2(col.bounds.size.x, PlayerHalfHeight + FrameDisplacement.magnitude);
        Vector2 boxDir = dir;

        Utility.DrawBox(origin, boxBounds, boxDir, Color.black);
        Collider2D overlap = Physics2D.OverlapBox(origin, boxBounds, Vector2Utility.GetVector2Angle(boxDir), sharedStats.collisionLayerMask);

        Collider2D sandOverlap = Physics2D.OverlapBox(rb.position, (Vector2)col.bounds.size + Vector2.one * INSIDE_SAND_ERROR_MARGIN, Vector2Utility.GetVector2Angle(boxDir), sharedStats.sandLayerMask);

        Physics2D.queriesStartInColliders = cachedStartInCol;
        Physics2D.queriesHitTriggers = cachedHitTriggers;

        bool insideBurrowSand = sandOverlap != null && sandOverlap.TryGetComponent(out ISand sand) && sand == entrySand;
        bool hitTerrain = overlap != null;
        bool canExit = !hitTerrain || !insideBurrowSand;

        if (canExit)
        {
            entrySand.OnSandExit(vel, rb.position);

            rb.position = rb.position + dir * PlayerHalfHeight;
            return new LandMovement.LandMovementTransition(dir, isDashing, entrySand);
        }

        return failedData;
    }


    #endregion

    private void ResetAllVelocities()
    {
        moveVel = Vector2.zero;
        dashVel = Vector2.zero;
        vel = Vector2.zero;
    }

    private void ApplyMovement()
    {
        vel = moveVel + dashVel;
        rb.linearVelocity = vel;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, Vector2Utility.GetUnityVector2Angle(MoveDir) - 90);
    }
}
