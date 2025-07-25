using UnityEngine;
using static TransitionLibrary;


public class RaceReadyMovement : IMovementState
{
    private readonly SandEntryMovementStats stats;
    private readonly Collider2D col;
    private readonly Rigidbody2D rb;

    private Vector2 startingPos;
    private int facingDir;

    public RaceReadyMovement(MovementInitData movementData)
    {
        col = movementData.Col;
        stats = movementData.Stats.interStateDashStats;
        rb = movementData.RB;

        startingPos = rb.position;

        IRaceController.OnRacePrepStart += () =>{
            startingPos = IRaceController.CurrentRace.StartingPos;

            facingDir = IRaceController.CurrentRace.FacingDir;
            raceEnterTriggered = true;
        };
        IRaceController.OnRaceStart += () => {
            raceStartTriggered = true;
        };
    }

    public void InitializeTransitions(PlayerStateMachine controller)
    {
        controller.AddTransition(GetType(), typeof(LandMovement), TransitionToLandMovement);
    }

    private bool raceStartTriggered;
    private bool raceEnterTriggered;
    public IStateSpecificTransitionData TransitionToLandMovement()
    {
        if (raceStartTriggered && raceEnterTriggered) return new LandMovement.LandMovementTransition(facingDir * Vector2.right, false, null, true);
        else return failedData;
    }

    public void EnterState(IStateSpecificTransitionData lastStateData)
    {
        rb.position = startingPos;
        rb.linearVelocity = Vector2.zero;
    }

    public void ExitState()
    {
        raceStartTriggered = false;
        raceEnterTriggered = false;
        Debug.Log("Exited race ready movement");
    }
}
