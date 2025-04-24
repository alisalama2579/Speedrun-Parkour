using UnityEngine;
using static TransitionLibrary;


public class RaceReadyMovement : IMovementState
{
    private readonly SandEntryMovementStats stats;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;

    public RaceReadyMovement(Rigidbody2D rb, Collider2D col, MovementStatsHolder stats)
    {
        this.col = col;
        this.stats = stats.interStateDashStats;
        this.rb = rb;
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

    public void Update(MovementInput _) 
    {
   
    }

    public void FixedUpdate()
    {
    }
}
