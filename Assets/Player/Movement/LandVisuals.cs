using System;
using System.Collections.Generic;
using UnityEngine;
using static TransitionLibrary;

public class LandVisuals : IMovementObserverState<LandMovement>
{
    private readonly AnimationStatsHolder stats;
    private readonly Animator anim;
    private readonly Transform transform;

    public LandMovement MovementState { get; set; }

    public LandVisuals(LandMovement landMovement, Transform transform, AnimationStatsHolder stats, Animator anim)
    {
        this.stats = stats;
        this.anim = anim;

        this.transform = transform;
        scale = new Vector2(transform.localScale.x, transform.localScale.y);
        MovementState = landMovement;
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
    }

    private bool isFacingRight;
    public int FacingDirection => isFacingRight ? 1 : -1;


    Vector2 scale;
    float progress;

    public void FixedUpdate()
    {
        fixedDeltaTime = Time.fixedDeltaTime;
    }
}