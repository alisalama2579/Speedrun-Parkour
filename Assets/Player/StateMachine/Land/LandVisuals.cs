using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.WSA;

public class LandVisuals : IMovementObserverState<LandMovement>
{
    private readonly AnimationStatsHolder stats;
    private readonly Animator anim;
    private readonly Transform transform;
    private readonly SpriteRenderer renderer;

    public LandMovement MovementState { get; set; }

    public LandVisuals(LandMovement landMovement, VisualsInitData visData)
    {
        anim = visData.Anim;
        transform = visData.Transform;
        stats = visData.Stats;
        renderer = visData.Renderer;
        MovementState = landMovement;

        MovementState.OnJump += (_) => {
            jumpTriggered = true;
        };
        MovementState.OnLeap += () => {
            leapTriggered = true;
        };
        MovementState.OnChangeGround += (grounded, impactForce, _) => {
            this.grounded = grounded;
            if (grounded)
                landTriggered = true;
        };
        MovementState.OnEntryLaunch += (launchDir) => {
            entryLaunchTriggered = true;
            renderer.flipX = launchDir.x > 0;
        };
        MovementState.OnRoll += (rollDir) => this.rollDir = rollDir;

        rollUnlockPredicate = new ConditionPredicate(RollExitCondition);

        scale = new Vector2(transform.localScale.x, transform.localScale.y);
        SetState(Idle);
    }
    public void ExitState()
    {
        time = 0;

        jumpTriggered = false;
        leapTriggered = false;
        landTriggered = false;
        isRolling = false;
        grounded = false;

        stateLocked = false;
        currentUnlockPredicate = null;
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, 0);

        SetState(Idle);
    }

    private bool stateLocked;
    private float timeLocked;
    private IPredicate currentUnlockPredicate;
    private readonly IPredicate rollUnlockPredicate;

    private float playerHorizontalSpeed;

    private bool grounded;
    private bool leapTriggered;
    private bool jumpTriggered;
    private bool landTriggered;
    private bool entryLaunchTriggered;

    private bool isRolling;
    private float rollDir;

    private MovementInput frameInput;
    private float deltaTime;
    private float timeStateChanged;
    private float time;

    Vector2 scale;

    public void UpdateState()
    {
        deltaTime = Time.deltaTime;
        time += deltaTime;
        this.frameInput = frameInput;

        playerHorizontalSpeed = Mathf.Abs(MovementState.HorizontalVel);
        isRolling = MovementState.IsRolling;

        var state = GetState();
        SetState(state);

        jumpTriggered = false;
        leapTriggered = false;
        landTriggered = false;
        entryLaunchTriggered = false;

        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y, GetZRotation());
        renderer.flipX = GetSpriteXFlip();
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
        if (entryLaunchTriggered) return EntryLaunch;
        if (isRolling) return LockState(Roll, rollUnlockPredicate);
        if (leapTriggered) return Leap;
        if (jumpTriggered) return Jump;
        if (landTriggered) return Land;
        if (grounded)
        {
            if (playerHorizontalSpeed > stats.minSpeedForRun)
                return Run;
            if (playerHorizontalSpeed > stats.minSpeedForWalk)
                return Walk;
            return Idle;
        }
        if (MovementState.VerticalVel < 0 && currentState != EntryLaunch && currentState != Leap) return Fall;

        return currentState;

        int LockState(int s, IPredicate unlockPredicate)
        {
            stateLocked = true;
            currentUnlockPredicate = unlockPredicate;
            return s;
        }
    }

    private float GetZRotation()
    {
        if (currentState == Leap || currentState == EntryLaunch) return Vector2Utility.GetUnityVector2Angle(MovementState.Vel) - 90;
        if (currentState == Roll) return transform.eulerAngles.z + stats.rollRotationSpeed * rollDir * deltaTime;

        return 0;
    }

    private bool GetSpriteXFlip()
    {
        if (currentState == Roll || currentState == EntryLaunch || currentState == Leap) return renderer.flipX;
        return frameInput.NonZeroHorizontalMove < 0;

        return renderer.flipX;
    }

    private Vector2 GetScale()
    {
        return scale;
    }


    #region Cached Properties

    private int currentState;

    private static readonly int Idle = Animator.StringToHash("Idle");
    private static readonly int Run = Animator.StringToHash("Run");
    private static readonly int Walk = Animator.StringToHash("Walk");
    private static readonly int Jump = Animator.StringToHash("Jump");
    private static readonly int Fall = Animator.StringToHash("Fall");
    private static readonly int Land = Animator.StringToHash("Land");
    private static readonly int Leap = Animator.StringToHash("Leap");
    private static readonly int EntryLaunch = Animator.StringToHash("EntryLaunch");
    private static readonly int Roll = Animator.StringToHash("Roll");

    #endregion


    private bool RollExitCondition() => leapTriggered || jumpTriggered || time > timeStateChanged + stats.rollAnimationTime;
}