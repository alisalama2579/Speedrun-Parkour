using UnityEngine;
using UnityEngine.Networking;

public class BurrowMovement : BaseMovementState
{
    private struct Input { public Vector2 Move; public bool DashHeld; }
    private readonly PlayerBurrowMovementStats stats;
    private Input frameInput;

    public BurrowMovement(Player player, ScriptableObject movementStats, PlayerControls controls, Rigidbody2D rb, Collider2D col) : base(player, movementStats, controls, rb, col)
    {
        this.player = player;
        this.controls = controls;
        this.col = col;

        stats = (PlayerBurrowMovementStats)movementStats;
        playerRB = rb;
    }
    public override void EnterState()
    {
    }

    protected override void HandleInput()
    {
        frameInput = new Input
        {
            Move = controls.PlayerBurrow.Move.ReadValue<Vector2>(),
            DashHeld = controls.PlayerBurrow.Dash.WasPressedThisFrame()
        };

        nonZeroMoveInput = frameInput.Move == Vector2.zero ? nonZeroMoveInput : frameInput.Move;
        dashRequested = frameInput.DashHeld || dashRequested;
    }


    private float deltaTime;
    private float time;

    public override void Update()
    {
        time += Time.deltaTime;
        HandleInput();
    }

    public override void UpdateMovement()
    {
        deltaTime = Time.deltaTime;

        HandleDash();
        HandleBounce();
        HandleBurrowMovement();

        ApplyMovement();
    }


    private Vector2 wishDir;
    private Vector2 vel;
    private Vector2 nonZeroMoveInput;
    private float GetVector2Angle(Vector2 dir) => Mathf.Rad2Deg * Mathf.Atan2(dir.y, dir.x);

    #region Bounce

    private Vector2 bounceVel;
    private float timeBounced = float.MinValue;
    private bool IsBouncing => time - timeBounced <= stats.bounceDuration;
    private void HandleBounce()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            playerRB.position,
            col.bounds.size,
            GetVector2Angle(currentDir),
            currentDir,
            stats.collisionDetectionDistance,
            stats.collisionLayerMask);

        bounceVel = Vector2.Lerp(bounceVel, Vector2.zero, stats.bounceDeceleration * deltaTime);

        if (!hit
            || !hit.transform.TryGetComponent(out TraversableTerrain _)
            || time - timeBounced < stats.BOUNCE_COOL_DOWN + stats.bounceDuration)
        { return; }

        ExecuteBounce(hit.normal);
    }

    private void ExecuteBounce(Vector2 hitNormal)
    {
        Vector2 reflectedVel = Vector2.Reflect(moveVel.normalized, hitNormal);

        currentDir = reflectedVel;
        bounceVel =  Vector2.Lerp(hitNormal, reflectedVel, stats.bounceNormalBias) * stats.bounceVel;
        Debug.Log(dashVel);
        dashPrevented = true;
        timeBounced = time;
    }

    #endregion


    #region Input Movement

    private Vector2 currentDir = Vector2.right;
    private Vector2 moveVel;
    private void HandleBurrowMovement()
    {
        wishDir = IsBouncing ? wishDir : nonZeroMoveInput;
        currentDir = Utility.Vector2Slerp(currentDir, wishDir, stats.rotationSpeed * dashControlMult * deltaTime);

        moveVel = stats.speed * deltaTime * currentDir;
    }

    #endregion


    #region Dash

    private bool dashRequested;
    private bool IsDashing => time - timeDashed <= stats.dashDuration;
    private float dashControlMult;

    private float timeDashed = float.MinValue;
    private bool dashPrevented;
    private Vector2 dashVel;
    private Vector2 targetDashVel;

    private void HandleDash()
    {
        bool canDash = !IsDashing && !dashPrevented;

        if (dashRequested && canDash) ExecuteDash();

        dashVel = Vector2.zero;
        dashControlMult = 1;
        dashRequested = false;

        if (dashPrevented || !IsDashing)
        {
            dashPrevented = false;
            return;
        }

        float timePercent = (time - timeDashed) / stats.dashDuration;

        float speedPercent = stats.dashSpeedCurve.Evaluate(timePercent);
        dashVel = targetDashVel * speedPercent;

        float controlPercent = stats.dashControlCurve.Evaluate(timePercent);
        dashControlMult = stats.dashControlMult * controlPercent;
    }

    private void ExecuteDash()
    {
        timeDashed = time;
        dashPrevented = false;

        targetDashVel = stats.dashVel * currentDir;
    }

    #endregion
    private void ApplyMovement()
    {
        vel = moveVel + bounceVel + dashVel;
        playerRB.linearVelocity = vel;

        player.transform.eulerAngles = new Vector3(player.transform.eulerAngles.x, player.transform.eulerAngles.y, GetVector2Angle(currentDir) + 90);
    }


    public override void ExitState()
    {
    }
}
