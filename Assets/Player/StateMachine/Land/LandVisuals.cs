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

    private MovementInput frameInput;
    private float HorizontalInput => frameInput.HorizontalMove;
    private float fixedDeltaTime;
    private float time;

    public void Update(MovementInput frameInput)
    {
        time += Time.deltaTime;
        if (MovementState.IsLeaping || MovementState.IsEntryLaunching)
        {
            transform.eulerAngles = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y,
               90 - Vector2Utility.GetVector2Angle(MovementState.Vel) + 90);
        }
        else transform.rotation = Quaternion.identity;
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