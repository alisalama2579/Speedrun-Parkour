using Unity.VisualScripting;
using UnityEngine;
using static TransitionLibrary;

public class BurrowVisuals : IMovementObserverState<BurrowMovement>
{
    public BurrowMovement MovementState { get; set; }

    private readonly Animator anim;
    private readonly AnimationStatsHolder stats;
    private readonly Transform transform;
    private readonly SpriteRenderer renderer;
    public BurrowVisuals(BurrowMovement burrowMovement, VisualsInitData visData)
    {
        anim = visData.Anim;
        transform = visData.Transform;
        stats = visData.Stats;
        renderer = visData.Renderer;

        MovementState = burrowMovement;
        SetState(Burrow);

        MovementState.OnBurrowDash += () =>{
            burrowDashTriggered = true;
        };
        dashUnlockPredicate = new ConditionPredicate(BurrowDashExitCondition);
    }

    public void ExitState() => Reset();

    private bool stateLocked;
    private float timeLocked;
    private IPredicate currentUnlockPredicate;
    private readonly IPredicate dashUnlockPredicate;

    private bool burrowDashTriggered;

    private float deltaTime;
    private float timeStateChanged;
    private float time;

    Vector2 scale;

    public void Update(MovementInput frameInput)
    {
        deltaTime = Time.deltaTime;
        time += deltaTime;

        var state = GetState();
        SetState(state);

        burrowDashTriggered = false;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, GetZRotation());

        if (MovementState.IsBurrowDashing) renderer.color = Color.blue;
        else renderer.color = Color.white;
    }

    private void SetState(int state, float duration = 0, int layer = 0)
    {
        if (state == currentState) return;

        timeStateChanged = time;
        anim.CrossFade(state, duration, layer);
        currentState = state;
    }

    private int GetState()
    {
        if (stateLocked) stateLocked = !currentUnlockPredicate.Test;
        if (stateLocked) return currentState;

        // Priorities
        if (burrowDashTriggered) return LockState(BurrowDash, dashUnlockPredicate);
        return Burrow;

        int LockState(int s, IPredicate unlockPredicate)
        {
            stateLocked = true;
            currentUnlockPredicate = unlockPredicate;
            return s;
        }
    }

    private float GetZRotation()
    {
        return Vector2Utility.GetUnityVector2Angle(MovementState.MoveDir) - 90;
    }

    private void Reset()
    {
        renderer.color = Color.white;
        time = 0;
        burrowDashTriggered = false;
    }

    #region Cached Properties

    private int currentState;
    private static readonly int Burrow = Animator.StringToHash("Burrow");
    private static readonly int BurrowDash = Animator.StringToHash("BurrowDash");

    #endregion

    private bool BurrowDashExitCondition() => time > timeStateChanged + stats.burrowDashTime || !MovementState.IsBurrowDashing;
}