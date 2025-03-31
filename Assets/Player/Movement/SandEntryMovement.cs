using UnityEngine;
using static TransitionLibrary;
using static UnityEditor.PlayerSettings;


public class SandEntryMovement : IState
{
    private readonly SandEntryMovementStats stats;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;

    public SandEntryMovement(PlayerControls controls, Rigidbody2D rb, Collider2D col, MovementStatsHolder stats)
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

    public void InitializeTransitions(MovementStateMachine controller)
    {
        controller.AddTransition(GetType(), typeof(BurrowMovement), TransitionToBurrow);
        controller.AddTransition(GetType(), typeof(LandMovement), TransitionToLand);
    }

    public void EnterState(IStateSpecificTransitionData lastStateData)
    {
        if (lastStateData is SandEntryData transitionData)
        {
            entrySand = transitionData.EntrySand;

            startingPoint = rb.position;
            exitPoint = transitionData.TargetPos;
            Vector2 diff = exitPoint - rb.position;
            dir = diff.normalized;
            duration = diff.magnitude / stats.entrySpeed;

            entrySand.OnSandTargetForBurrow(dir * stats.entrySpeed);
            if (entrySand is SandBall ball)
                durationToSandTouch = (exitPoint - 2 * ball.GetComponent<CircleCollider2D>().radius * dir).magnitude / stats.entrySpeed;
            else
                durationToSandTouch = 0.9f * duration;
        }
    }

    public void ExitState()
    {
        t = 0;
        sandTouched = false;
    }

    private Vector2 exitPoint;
    private Vector2 startingPoint;
    private Vector2 dir;
    private Vector2 pos;
    private ISand entrySand;
    float durationToSandTouch;
    bool sandTouched;
    float duration;

    public void Update(Player.Input _) 
    {
        if (!sandTouched && t >= durationToSandTouch)
        {
            sandTouched = true;
            entrySand.OnSandBurrowEnter(dir * stats.entrySpeed, pos);
        }
    }

    public void HandleInput(Player.Input _) { }
    float t;

    public void UpdateMovement()
    {
        rb.linearVelocity = Vector2.zero;
        t += Time.deltaTime;

        pos = Vector2.Lerp(startingPoint, exitPoint, t/duration);
        col.transform.position = new Vector3(pos.x, pos.y, col.transform.position.z);
    }

    public IStateSpecificTransitionData TransitionToBurrow()
    {
        if(t >= duration && entrySand is BurrowSand)
        {
            entrySand.OnSandBurrowExit(dir * stats.entrySpeed, pos);
            return new BurrowMovement.BurrowMovementTransitionData(dir, exitPoint);
        }
        return failedData;
    }

    public IStateSpecificTransitionData TransitionToLand()
    {
        if (t >= duration && entrySand is not BurrowSand)
        {
            entrySand.OnSandBurrowExit(dir * stats.entrySpeed, pos);
            return new LandMovement.LandMovementTransition(dir, true);
        }
        return failedData;
    }
}
