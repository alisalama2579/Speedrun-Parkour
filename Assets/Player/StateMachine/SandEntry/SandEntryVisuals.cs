using System;
using UnityEngine;
using static TransitionLibrary;

public class SandEntryVisuals : IMovementObserverState<SandEntryMovement>
{
    private readonly Animator anim;
    private readonly AnimationStatsHolder stats;
    private readonly Transform transform;
    private readonly SpriteRenderer renderer;

    public SandEntryMovement MovementState { get; set; }

    public SandEntryVisuals(SandEntryMovement sandEntryMovement, VisualsInitData visData)
    {
        anim = visData.Anim;
        transform = visData.Transform;
        stats = visData.Stats;
        renderer = visData.Renderer;

        MovementState = sandEntryMovement;
    }

    public void InitializeTransitions(PlayerStateMachine controller)
    {
    }

    public void EnterState(IStateSpecificTransitionData lastStateData)
    {
        transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y,
        90 - Vector2Utility.GetVector2Angle(MovementState.Dir) + 90);
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