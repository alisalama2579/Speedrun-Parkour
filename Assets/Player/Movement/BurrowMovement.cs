using UnityEngine;

public class BurrowMovement : BaseMovementState
{
    private struct Input { public Vector2 Move; }

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
        frameInput = new Input{  Move = controls.PlayerBurrow.Move.ReadValue<Vector2>()};
        if (frameInput.Move != Vector2.zero && !IsBouncing) targetDirection = frameInput.Move;
    }

    private readonly PlayerBurrowMovementStats stats;
    private Input frameInput;
    private float fixedDeltaTime;
    private float time;

    public override void Update()
    {
        time += Time.deltaTime;
        HandleInput();
    }

    private Vector2 targetDirection;
    private Vector2 velocity;
    private float GetVector2Angle(Vector2 dir) => Mathf.Rad2Deg * Mathf.Atan2(dir.y, dir.x);
    private Vector2 GetAngleVector2(float angle) => new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)).normalized;

    public override void UpdateMovement()
    {
        fixedDeltaTime = Time.fixedDeltaTime;

        HandleWallBounce();
        HandleBurrowMovement();

        ApplyMovement();
    }

    #region Bounce

    private Vector2 bounceVelocity;
    private float timeBounced;
    private bool IsBouncing => time - timeBounced <= stats.wallBounceDuration;
    private void HandleWallBounce()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            playerRB.position,
            col.bounds.size,
            currentAngle,
            velocity.normalized,
            stats.collisionDetectionDistance,
            stats.collisionLayerMask);

        bounceVelocity = Vector2.Lerp(bounceVelocity, Vector2.zero, (time - timeBounced) / stats.wallBounceDuration);

        if (IsBouncing || !hit || !hit.transform.TryGetComponent(out TraversableTerrain _)) return;

        Vector2 reflectedVel = Vector2.Reflect(velocity.normalized, hit.normal);
        Debug.DrawLine(playerRB.position, playerRB.position + reflectedVel * 10, Color.green, 0.1f);

        currentAngle = GetVector2Angle(reflectedVel);
        targetDirection = reflectedVel;
        bounceVelocity = reflectedVel * stats.wallBounceStrength;
        timeBounced = time;
    }

    #endregion

    #region Input Movement

    private float currentAngle;
    private void HandleBurrowMovement()
    {
        if (frameInput.Move != Vector2.zero && !IsBouncing) targetDirection = frameInput.Move;

        float targetAngle = GetVector2Angle(targetDirection);
        currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, stats.rotationSpeed * fixedDeltaTime);

        velocity = stats.speed * fixedDeltaTime * GetAngleVector2(currentAngle);
    }

    #endregion

    private void ApplyMovement()
    {
        player.transform.eulerAngles = new Vector3(player.transform.eulerAngles.x, player.transform.eulerAngles.y, currentAngle + 90);

        playerRB.linearVelocity = velocity + bounceVelocity;
    }


    public override void ExitState()
    {
    }
}
