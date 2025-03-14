using Unity.VisualScripting;
using UnityEngine;
using static TransitionLibrary;


public class InterStateDashMovement : IState
{
    private readonly BurrowMovementStats stats;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;

    public InterStateDashMovement(PlayerControls controls, Rigidbody2D rb, Collider2D col, MovementStatsHolder stats)
    {
        this.col = col;
        this.stats = stats.burrowStats;
        this.rb = rb;
    }

    public class InterStateDashData : AnyTransitionData
    {
        public Vector2 ExitPos { get; }
        public InterStateDashData(Vector2 exitPos) => ExitPos = exitPos;
    }

    public void InitializeTransitions(MovementStateMachine controller)
    {
        controller.AddTransition(GetType(), typeof(BurrowMovement), TransitionToBurrow);
    }

    public void EnterState(IStateSpecificTransitionData lastStateData)
    {
        if (lastStateData is InterStateDashData transitionData)
        {
            entryPoint = rb.position;
            exitPoint = transitionData.ExitPos;
            Vector2 diff = exitPoint - rb.position;
            dir = diff.normalized;
            dist = diff.magnitude;
            duration = dist / speed;
        }
    }

    public void ExitState()
    {
        t = 0;
    }

    private Vector2 exitPoint;
    private Vector2 entryPoint;
    private Vector2 dir;
    float dist;
    float speed = 150;
    float duration;

    public void Update(Player.Input _)
    {
    }

    public void HandleInput(Player.Input _)
    {
    }

    float t;
    public void UpdateMovement()
    {
        rb.linearVelocity = Vector2.zero;
        t += Time.deltaTime;

        Vector2 pos = Vector2.Lerp(entryPoint, exitPoint, t/duration);
        col.transform.position = new Vector3(pos.x, pos.y, col.transform.position.z);
    }


    public IStateSpecificTransitionData TransitionToBurrow()
    {
        if(t >= duration) return new BurrowMovement.BurrowMovementTransitionData(dir, exitPoint);
        return failedData;
    }
}
