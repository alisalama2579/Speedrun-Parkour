using System;
using UnityEngine;
using static TransitionLibrary;

public class BurrowVisuals : IMovementObserverState<BurrowMovement>
{
    public BurrowMovement MovementState { get; set; }

    private readonly Animator anim;
    private readonly AnimationStatsHolder stats;
    private BurrowMovement burrowMovement;

    public BurrowVisuals(BurrowMovement burrowMovement, Transform transform, AnimationStatsHolder animationStats, Animator anim)
    {
        this.anim = anim;
        stats = animationStats;

        MovementState = burrowMovement;
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

    private MovementInput frameInput;
    private float HorizontalInput => frameInput.HorizontalMove;
    private float fixedDeltaTime;
    private float time;

    public void Update(MovementInput frameInput)
    {
        time += Time.deltaTime;
        HandleInput(frameInput);
    }

    public void HandleInput(MovementInput frameInput)
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