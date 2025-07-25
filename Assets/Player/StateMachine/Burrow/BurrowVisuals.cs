using Unity.VisualScripting;
using UnityEngine;
using static TransitionLibrary;

public class BurrowVisuals : IMovementObserverState<BurrowMovement>
{
    public BurrowMovement MovementState { get; set; }

    private readonly Animator anim;
    private readonly AnimationStatsHolder stats;
    private readonly Transform transform;
    public BurrowVisuals(BurrowMovement burrowMovement, VisualsInitData visData)
    {
        anim = visData.Anim;
        transform = visData.Transform;
        stats = visData.Stats;

        MovementState = burrowMovement;
        SetState(Burrow);

        MovementState.OnBurrowDash += () =>{
            burrowDashTriggered = true;
        };
        dashUnlockPredicate = new ConditionPredicate(BurrowDashExitCondition);
    }

    public void ExitState()
    {
        stateLocked = false;
        currentUnlockPredicate = null;
        time = 0;
        burrowDashTriggered = false;
        SetState(Idle);
    }

    private bool stateLocked;
    private float timeLocked;
    private IPredicate currentUnlockPredicate;
    private readonly IPredicate dashUnlockPredicate;

    private bool burrowDashTriggered;

    private float deltaTime;
    private float timeStateChanged;
    private float time;

    Vector2 scale;

    public void UpdateState()
    {
        deltaTime = Time.deltaTime;
        time += deltaTime;

        var state = GetState();
        SetState(state);

        burrowDashTriggered = false;

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
        if (stateLocked) stateLocked = currentUnlockPredicate != null && !currentUnlockPredicate.Test;
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

    #region Cached Properties

    private int currentState;
    private static readonly int Burrow = Animator.StringToHash("Burrow");
    private static readonly int BurrowDash = Animator.StringToHash("BurrowDash");
    private static readonly int Idle = Animator.StringToHash("Idle");

    #endregion

    private bool BurrowDashExitCondition() => !MovementState.IsBurrowDashing;
}