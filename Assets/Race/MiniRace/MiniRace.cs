using System;
using System.Collections.Generic;
using UnityEngine;
using static TransitionLibrary;

public class MiniRace : MonoBehaviour, IStateMachine, IRaceController
{
    [SerializeField] private MiniRacePrepState prepState;
    [SerializeField] private InMiniRaceState inRaceState;
    [SerializeField] private MiniObjectiveCompletedState objectiveCompletedState;
    [SerializeField] private NotInMiniRaceState notInRaceState;

    [Space(5)]

    [SerializeField] private Transform startingT;

    public Vector2 StartingPos => (Vector2)startingT.position;
    public int FacingDir => facingRight ? 1 : -1;
    [SerializeField] private bool facingRight;
    [SerializeField] private List<RaceGhostInfo> raceGhosts;

    public double TargetGhostTime { get; set; }
    private double fastestGhostTime;
    private double fastestPlayerTime;

    public PlayerRaceStats RaceStats { get; set; }
    private bool passed;
    private bool perfected;
    private int attempts;

    private double timeRaceStarted;
    private double timeRaceObjectiveCompleted;


    public void Awake()
    {
        Nodes = new();
        AnyTransitions = new();

        AddNode(typeof(MiniRacePrepState), prepState);
        AddNode(typeof(InMiniRaceState), inRaceState);
        AddNode(typeof(MiniObjectiveCompletedState), objectiveCompletedState);
        AddNode(typeof(NotInMiniRaceState), notInRaceState);

        Initializetransitions();
        SetStartingState(typeof(NotInMiniRaceState));

        UpdateTargetGhost();

        prepState.OnEnter += () =>
        {
            IRaceController.CurrentRace = this;
            IRaceController.OnRacePrepStart?.Invoke();
        };
        inRaceState.OnEnter += () =>
        {
            attempts++;
            timeRaceStarted = Time.timeAsDouble;

            IRaceController.OnRaceStart?.Invoke();
        };
        objectiveCompletedState.OnEnter += () =>
        {
            timeRaceObjectiveCompleted = Time.timeAsDouble;
            double newPlayerTime = timeRaceObjectiveCompleted - timeRaceStarted;

            UpdateFastestGhosts(newPlayerTime);
            UpdateTargetGhost();

            UpdatePlayerRaceStats(newPlayerTime);

            IRaceController.OnCompleteRaceObjective?.Invoke();
        };
        notInRaceState.OnEnter += () => { IRaceController.CurrentRace = null; };
        
    }

    public void Update()
    {
        Current?.UpdateState();
    }

    public void FixedUpdate()
    {
        Current?.FixedUpdate();

        var transition = GetTransition(out IStateSpecificTransitionData transitionData);
        if (transition != null) SwitchMovementState(transition.To, transitionData);
    }
    private void UpdateFastestGhosts(double newPlayerTime)
    {
        if (raceGhosts == null) return;

        double bestTime = double.MaxValue;
        for (int i = 0; i < raceGhosts.Count; i++)
        {
            var ghost = raceGhosts[i];

            if(ghost.raceTime >= newPlayerTime && raceGhosts.Count > 1) raceGhosts.Remove(ghost);
            else if (ghost)
            {
                double ghostTime = raceGhosts[i].raceTime;
                if (ghostTime < bestTime)
                    bestTime = ghostTime;
            }
        }

        fastestGhostTime = bestTime;
    }
    private void UpdateTargetGhost()
    {
        for (int i = 0; i < raceGhosts.Count; i++)
        {
            var ghost = raceGhosts[i];

            if (ghost == null) raceGhosts.Remove(ghost);
            else
            {
                TargetGhostTime = ghost.raceTime;
                break;
            }
        }
    }

    private void UpdatePlayerRaceStats(double newPlayerTime)
    {
        if (newPlayerTime > fastestPlayerTime) fastestPlayerTime = newPlayerTime;
        passed = fastestPlayerTime <= TargetGhostTime;
        perfected = fastestPlayerTime <= fastestGhostTime;

        RaceStats = new()
        {
            Passed = passed,
            Perfected = perfected,
            Attempts = attempts,
            RecordTime = fastestPlayerTime
        };
    }

    #region StateMachine
    public StateNode Current { get; set; }
    public IState CurrentState => Current.State;

    public Dictionary<Type, StateNode> Nodes { get; set; }
    public HashSet<Transition> AnyTransitions { get; set; }


    public void AddTransition(Type from, Type to, Func<IStateSpecificTransitionData> func)
     => GetNode(from).AddTransition(GetNode(to).State, func);
    public void AddUnconditionalTransition(Type from, Type to)
        => GetNode(from).AddTransition(GetNode(to).State, AnyTransitionFunc);
    public void AddAnyTransition(Type to, Func<IStateSpecificTransitionData> func)
        => AnyTransitions.Add(new Transition(GetNode(to).State, func));


    public T GetStateObject<T>() where T : class, IState => (GetNode(typeof(T)).State) as T;

    public StateNode GetNode(Type type) => Nodes.GetValueOrDefault(type);
    public void AddNode(Type type, IState state) => Nodes.Add(type, new StateNode(state));

    Transition GetTransition(out IStateSpecificTransitionData transitionData)
    {
        foreach (var transition in AnyTransitions)
        {
            if (transition.TryExecute(out transitionData))
                return transition;
        }
        foreach (var transition in Current.Transitions)
        {
            if (transition.TryExecute(out transitionData))
                return transition;
        }

        transitionData = failedData;
        return null;
    }

    public void Initializetransitions ()
    {
        foreach (StateNode stateNode in Nodes.Values) stateNode.State.InitializeTransitions(this);
    }

    public void SetStartingState(Type startingType)
    {
        Current = GetNode(startingType);
        Current?.EnterState(failedData);
    }

    public void SwitchMovementState(IState state, IStateSpecificTransitionData transitionData)
    {
        if (Current != null && state == Current.State) return;

        Current?.ExitState();

        Current = GetNode(state.GetType());

        Current?.EnterState(transitionData);
    }
    #endregion
}
