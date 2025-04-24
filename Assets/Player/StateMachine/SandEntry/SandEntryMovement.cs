using UnityEngine;
using static TransitionLibrary;


public class SandEntryMovement : IMovementState
{
    private readonly SandEntryMovementStats stats;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;

    public SandEntryMovement(Rigidbody2D rb, Collider2D col, MovementStatsHolder stats)
    {
        this.col = col;
        this.stats = stats.interStateDashStats;
        this.rb = rb;
    }

    public class SandEntryData : SuccesfulTransitionData
    {
        public Vector2 TargetPos { get; }
        public ISand EntrySand { get; }
        public SandEntryData(Vector2 targetPos,  ISand sand) 
        {
            TargetPos = targetPos;
            EntrySand = sand;
        } 
    }

    public void InitializeTransitions(PlayerStateMachine controller)
    {
        controller.AddTransition(GetType(), typeof(BurrowMovement), TransitionToBurrow);
        controller.AddTransition(GetType(), typeof(LandMovement), TransitionToLand);
    }

    public void EnterState(IStateSpecificTransitionData lastStateData)
    {
        if (lastStateData is SandEntryData transitionData)
        {
            entrySand = transitionData.EntrySand;

            pos = startingPoint = rb.position;
            exitPoint = transitionData.TargetPos;
            Vector2 diff = exitPoint - rb.position;
            dir = diff.normalized;
            duration = diff.magnitude / stats.entrySpeed;

            targetIsBurrowSand = entrySand is BurrowSand;
            entrySand.OnSandTargetForBurrow(dir * stats.entrySpeed);

            if (entrySand is SandBall ball)
                durationToSandTouch = duration - (ball.GetComponent<CircleCollider2D>().radius / stats.entrySpeed);
            else
                durationToSandTouch = 0.9f * duration;
        }
    }

    public void ExitState()
    {
        CheckSandEntryInvokation();
        t = 0;
        sandTouched = false;
    }

    private Vector2 exitPoint;
    private Vector2 startingPoint;
    private Vector2 dir;
    public Vector2 Dir => dir;

    private Vector2 pos;
    private ISand entrySand;
    float durationToSandTouch;
    bool sandTouched;
    float duration;

    bool targetIsBurrowSand;

    public void Update(MovementInput _) 
    {
        t += Time.deltaTime;
        CheckSandEntryInvokation();
    }

    private void CheckSandEntryInvokation()
    {
        if (!targetIsBurrowSand && !sandTouched && t >= durationToSandTouch)
        {
            sandTouched = true;
            entrySand.OnSandExit(dir * stats.entrySpeed, pos);
            EventsHolder.PlayerEvents.OnPlayerEnterSand?.Invoke(entrySand);
        }
    }

    public void HandleInput(MovementInput _) { }
    float t;

    public void FixedUpdate()
    {
        rb.linearVelocity = Vector2.zero;

        pos = Vector2.Lerp(startingPoint, exitPoint, t/duration);
        col.transform.position = new Vector3(pos.x, pos.y, col.transform.position.z);
    }


    public IStateSpecificTransitionData TransitionToBurrow()
    {
        if(t >= duration && targetIsBurrowSand)
        {
            return new BurrowMovement.BurrowMovementTransitionData(dir, exitPoint, entrySand);
        }
        return failedData;
    }


    public IStateSpecificTransitionData TransitionToLand()
    {
        if (t >= duration && !targetIsBurrowSand)
        {
            entrySand.OnSandEnter(dir * stats.entrySpeed, pos);
            EventsHolder.PlayerEvents.OnPlayerExitSand?.Invoke(entrySand);

            return new LandMovement.LandMovementTransition(dir, true, entrySand);
        }
        return failedData;
    }
}
