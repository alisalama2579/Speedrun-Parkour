using System;
using UnityEngine;
using static TransitionLibrary;

public class SandEntryVisuals : IMovementObserverState<SandEntryMovement>
{
    private readonly Animator anim;
    private readonly AnimationStatsHolder stats;
    public SandEntryMovement MovementState { get; set; }

    public SandEntryVisuals(SandEntryMovement sandEntryMovement, Transform transform, AnimationStatsHolder animationStats, Animator anim)
    {
        this.anim = anim;
        stats = animationStats;

        MovementState = sandEntryMovement;
    }

    public void InitializeTransitions(PlayerStateMachine controller)
    {
    }

    public void EnterState(IStateSpecificTransitionData lastStateData)
    {
    }

    public void ExitState()
    {
    }

    private Player.Input frameInput;
    private float HorizontalInput => frameInput.HorizontalMove;
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
    }


    public void FixedUpdate()
    {
        fixedDeltaTime = Time.fixedDeltaTime;

    }

    private bool isFacingRight;
    public int FacingDirection => isFacingRight ? 1 : -1;
}