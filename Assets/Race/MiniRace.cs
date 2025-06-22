using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiniRace : MonoBehaviour, IRaceController
{
    [SerializeField] private Transform startingT;
    [SerializeField] private RaceStart raceStart;
    [SerializeField] private RaceEnd raceEnd;
    [SerializeField] private List<RaceGhost> raceGhosts;
    [SerializeField] private float countdownTime;
    private const int MINI_RACE_COUNTDOWNS = 3;
    public int CountDowns => MINI_RACE_COUNTDOWNS;
    public Vector2 StartingPos => (Vector2)startingT.position;

    #region Race Logic Variables
    [SerializeField] private BooleanProperty playerInPositionProperty;

    public RaceState CurrentState => raceState;
    public PlayerRaceStats RaceStats => playerRaceStats;

    private RaceState raceState = RaceState.NotInRace;


    private PlayerRaceStats playerRaceStats;

    private IPredicate playerPassPredicate;
    private IPredicate playerPerfectPredicate;
    private IPredicate raceEnterPredicate;
    private IPredicate raceStartPredicate;
    private IPredicate raceExitPredicate;
    private IPredicate objectiveCompletePredicate;

    private int attempts;
    private bool passed;
    private bool perfected;
    private double timeRaceStarted;
    private double timeRaceEnded;
    private double fastestPlayerTime;

    private double fastestGhostTime;
    #endregion


    private void OnValidate() => UpdateFastestGhosts();
    private void Start()
    {
        playerPassPredicate = new ConditionPredicate(PlayerPassPredicate);
        playerPerfectPredicate = new ConditionPredicate(PlayerPerfectPredicate);
        raceEnterPredicate = new ConditionPredicate(RaceEnterPredicate);
        raceStartPredicate = new ConditionPredicate(RaceStartPredicate);
        raceExitPredicate = new ConditionPredicate(RaceExitPredicate);
        objectiveCompletePredicate = new ConditionPredicate(ObjectiveCompletePredicate);

        raceStart.onPlayerEnter += () => 
        { 
            playerTriggeredRaceEnter = true;
        };
        raceEnd.onPlayerEnter += () => { playerTriggeredRaceCompletion = true; };
        
    }
    public void ResetRace()
    {
        RaceExited();
        RaceStarted();
    }

    private float timeRaceEntered;
    public void RaceEntered()
    {
        playerTriggeredRaceEnter = false;
        timeRaceEntered = Time.timeSinceLevelLoad;
        StartCoroutine(Prep());

        RaceState newState = RaceState.Prep;
        if (raceState != newState) Debug.Log("Race state is now " + newState);
        else Debug.Log("Race state remained as " + newState);
        raceState = newState;

        IRaceController.OnRaceEnter?.Invoke(this);
        IRaceController.currentRace = this;
    }
    public void RaceObjectiveCompleted()
    {
        playerTriggeredRaceCompletion = false;

        RaceState newState = RaceState.CompletedObjective;
        if (raceState != newState) Debug.Log("Race state is now " + newState);
        else Debug.Log("Race state remained as " + newState);
        raceState = newState;

        double time = Time.timeAsDouble;
        timeRaceEnded = time;

        double newTime = time - timeRaceStarted;
        if (newTime < fastestPlayerTime) fastestPlayerTime = newTime;

        if (!passed && playerPassPredicate.Test) passed = true;
        if (!perfected && playerPerfectPredicate.Test) perfected = true;

        UpdatePlayerRaceStats();
        raceExitRequested = true;

        IRaceController.OnCompleteRaceObjective?.Invoke();
    }
    public void RaceStarted()
    {
        prepEnded = false;

        RaceState newState = RaceState.InRace;
        if (raceState != newState) Debug.Log("Race state is now " + newState);
        else Debug.Log("Race state remained as " + newState);
        raceState = newState;

        attempts++;
        timeRaceStarted = Time.timeAsDouble;

        UpdatePlayerRaceStats();
        IRaceController.OnRaceStart?.Invoke();
    }
    public void RaceExited()
    {
        raceExitRequested = false;

        RaceState newState = RaceState.NotInRace;
        if (raceState != newState) Debug.Log("Race state is now " + newState);
        else Debug.Log("Race state remained as " + newState);
        raceState = newState;

        IRaceController.OnRaceExit?.Invoke();
        IRaceController.currentRace = null;
    }

    private void Update()
    {
        playerInPosition = playerInPositionProperty == null || playerInPositionProperty.Value;

        HandleRaceState();
    }

    private void HandleRaceState()
    {
        if (raceExitPredicate.Test) { RaceExited();}
        if (raceEnterPredicate.Test) { RaceEntered(); }
        if (raceStartPredicate.Test) { RaceStarted();}
        if (objectiveCompletePredicate.Test) { RaceObjectiveCompleted();}
    }

    private void UpdatePlayerRaceStats()
    {
        playerRaceStats = new()
        {
            Passed = passed,
            Perfected = perfected,
            Attempts = attempts,
            RecordTime = fastestPlayerTime
        };
    }




    private bool PlayerPassPredicate() => raceState == RaceState.CompletedObjective;

    private bool PlayerPerfectPredicate() => timeRaceEnded - timeRaceStarted < fastestGhostTime && raceState == RaceState.CompletedObjective;

    private bool playerInPosition;
    private bool RaceStartPredicate() => raceState == RaceState.Prep && prepEnded;

    private bool playerTriggeredRaceEnter;
    private bool RaceEnterPredicate() => raceState == RaceState.NotInRace && playerInPosition && playerTriggeredRaceEnter;

    private bool raceExitRequested;
    private bool RaceExitPredicate() => raceState != RaceState.NotInRace && raceExitRequested;

    private bool playerTriggeredRaceCompletion;
    private bool ObjectiveCompletePredicate() => raceState == RaceState.InRace && playerTriggeredRaceCompletion;
    private bool ReadyForCountDownPredicate() => true || Time.timeSinceLevelLoad - timeRaceEntered >= IRaceController.ABSOLUTE_MAX_PREP_TIME;

    private bool prepEnded;
    private IEnumerator Prep()
    {
        yield return new WaitUntil(ReadyForCountDownPredicate);
        Debug.Log("Race prep to countdown complete");

        for(int i = 0; i < CountDowns; i++)
        {
            yield return new WaitForSeconds(countdownTime);
            Debug.Log("Race prep countdown number " + i);
            IRaceController.OnCountDown?.Invoke(i);
        }
        Debug.Log("Race prep ended");
        prepEnded = true;
    }

    private void UpdateFastestGhosts()
    {
        if (raceGhosts == null) return;

        float bestTime = float.MaxValue;
        for(int i = 0; i < raceGhosts.Count; i++)
        {
            float ghostTime = raceGhosts[i].raceTime;
            if (bestTime < ghostTime)
                bestTime = ghostTime;
        }

        fastestGhostTime = bestTime;
    }
    private void RemoveGhosts(double targetTime)
    {
        //"Remove" ghosts below this time, still unsure what that means
        UpdateFastestGhosts();
    }

}


