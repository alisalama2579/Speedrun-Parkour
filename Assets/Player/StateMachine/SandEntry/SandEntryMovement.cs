using UnityEngine;
using static TransitionLibrary;


public class SandEntryMovement : IMovementState
{
    private readonly SandEntryMovementStats stats;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;
    private readonly Transform transform;
    private readonly PlayerMovementProperties properties;

    public SandEntryMovement(MovementInitData movementData)
    {
        col = movementData.Col;
        stats = movementData.Stats.interStateDashStats;
        rb = movementData.RB;
        transform = movementData.Transform;
        properties = movementData.Properties;
    }

    public class SandEntryData : SuccesfulTransitionData
    {
        public Vector2 TargetPos { get; }
        public ISand EntrySand { get; }
        public Vector2 Vel { get; }
        public SandEntryData(Vector2 targetPos,  ISand sand, Vector2 vel) 
        {
            TargetPos = targetPos;
            EntrySand = sand;
            Vel = vel;
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

            speed = Mathf.Max(transitionData.Vel.magnitude * stats.velToSpeedRatio, stats.entrySpeed);
            duration = diff.magnitude / speed;

            targetIsBurrowSand = entrySand is BurrowSand;
            entrySand.OnSandTargetForBurrow(dir * speed);

            if (entrySand is SandBall ball)
                durationToSandTouch = duration - (ball.GetComponent<CircleCollider2D>().radius / speed);
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
    public Vector2 dir;
    public Vector2 Dir => dir;

    private Vector2 pos;
    private ISand entrySand;
    float durationToSandTouch;
    float speed;
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
            entrySand.OnSandExit(dir * speed, pos);
            EventsHolder.PlayerEvents.OnPlayerEnterSand?.Invoke(entrySand);
        }
    }

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
            return new BurrowMovement.BurrowMovementTransitionData(dir, exitPoint, entrySand, false);
        }
        return failedData;
    }


    public IStateSpecificTransitionData TransitionToLand()
    {
        if (t >= duration && !targetIsBurrowSand)
        {
            entrySand.OnSandEnter(dir * speed, pos);
            EventsHolder.PlayerEvents.OnPlayerExitSand?.Invoke(entrySand);

            return new LandMovement.LandMovementTransition(dir, true, entrySand);
        }
        return failedData;
    }
}
