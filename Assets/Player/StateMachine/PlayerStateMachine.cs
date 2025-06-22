using System;
using System.Collections.Generic;
using UnityEngine;
using static TransitionLibrary;

public class PlayerStateMachine
{
    public void AddTransition(Type from, Type to, Func<IStateSpecificTransitionData>  func)
        => GetNode(from).AddTransition(GetNode(to).MovementState, func);
    public void AddUnconditionalTransition(Type from, Type to)
        => GetNode(from).AddTransition(GetNode(to).MovementState, AnyTransitionFunc);
    public void AddAnyTransition(Type to, Func<IStateSpecificTransitionData> func) 
        => anyTransitions.Add(new Transition(GetNode(to).MovementState, func));


    public T GetStateObject<T>() where T : class, IMovementState => (GetNode(typeof(T)).MovementState) as T;

    StateNode GetNode(Type type) => nodes.GetValueOrDefault(type);
    void AddNode(Type type, IMovementState movementState, IState visualState, IState soundState) => nodes.Add(type, new StateNode(movementState, visualState, soundState));

    Transition GetTransition(out IStateSpecificTransitionData transitionData)
    {
        foreach (var transition in anyTransitions)
        {
            if (transition.TryExecute(out transitionData))
                return transition;
        }

        foreach (var transition in current.Transitions)
        {
            if (transition.TryExecute(out transitionData))
                return transition;
        }

        transitionData = failedData;
        return null;
    }


    private StateNode current;
    Dictionary<Type, StateNode> nodes = new();
    HashSet<Transition> anyTransitions = new();

    public PlayerStateMachine(Type startingType, MovementInitData movementData, VisualsInitData visData, SoundInitData soundData)
    {
        LandMovement landMovement = new LandMovement(movementData);
        AddNode(typeof(LandMovement),
            landMovement,
            new LandVisuals(landMovement, visData), 
            new LandSound(landMovement, soundData));

        BurrowMovement burrowMovement = new BurrowMovement(movementData);
        AddNode(typeof(BurrowMovement), 
            burrowMovement, 
            new BurrowVisuals(burrowMovement, visData), 
            new BurrowSound(burrowMovement, soundData)
            );

        SandEntryMovement sandEntryMovement = new SandEntryMovement(movementData);
        AddNode(typeof(SandEntryMovement), 
            sandEntryMovement, 
            new SandEntryVisuals(sandEntryMovement, visData),
            new SandEntrySound(sandEntryMovement, soundData));

        RaceReadyMovement raceReadyMovement = new RaceReadyMovement(movementData);
        AddNode(typeof(RaceReadyMovement),
            raceReadyMovement,
            null, null);

        InitializeStateTransitions();
        SetStartingState(startingType);
    }

    private void InitializeStateTransitions() { foreach (StateNode stateNode in nodes.Values) stateNode.MovementState.InitializeTransitions(this); }
    private void SetStartingState(Type startingType)
    {
        current = GetNode(startingType);
        current?.EnterState(failedData);
    }

    private void SwitchMovementState(IState state, IStateSpecificTransitionData transitionData) 
    {
        if (current != null && state == current.MovementState) return;

        current?.ExitState();

        current = GetNode(state.GetType());

        current?.EnterState(transitionData);
    }

    public void Update(MovementInput frameInput)
    {
        current?.Update(frameInput);
    }

    public void FixedUpdate() 
    {
        current?.FixedUpdate();

        var transition = GetTransition(out IStateSpecificTransitionData transitionData);
        if (transition != null) SwitchMovementState(transition.To, transitionData);
    }

    public void OnDestroy()
    {
        foreach (StateNode node in nodes.Values)
        {
            foreach (Transition transition in node.Transitions)
                transition.ClearEvent();
        }
    }

    #region Collisions

    public void CollisionEnter(Collision2D collision) => current.MovementState?.CollisionEnter(collision);
    public void CollisionExit(Collision2D collision) => current.MovementState?.CollisionExit(collision);
    public void TriggerEnter(Collider2D trigger) => current.MovementState?.TriggerEnter(trigger); 
    public void TriggerExit(Collider2D trigger) => current.MovementState?.TriggerExit(trigger); 

    public void CollisionEnter(IPlayerCollisionInteractor collisionListener) => current.MovementState?.CollisionEnter(collisionListener);
    public void CollisionExit(IPlayerCollisionInteractor collisionListener) => current.MovementState?.CollisionExit(collisionListener);
    public void TriggerEnter(IPlayerCollisionInteractor collisionListener) => current.MovementState?.TriggerEnter(collisionListener);
    public void TriggerExit(IPlayerCollisionInteractor collisionListener) => current.MovementState?.TriggerExit(collisionListener);

    #endregion

    class StateNode : IState
    {
        public IMovementState MovementState { get; }
        public IState VisualsState { get; }
        public IState SoundsState { get; }
        public HashSet<Transition> Transitions { get; }

        public StateNode(IMovementState movementState, IState visualsState, IState soundsState)
        {
            Transitions = new HashSet<Transition>();

            MovementState = movementState;
            VisualsState = visualsState;
            SoundsState = soundsState;
        }

        public void AddTransition(IMovementState to, Func<IStateSpecificTransitionData> func) 
        {
            Transitions.Add(new Transition(to, func));
        }

        public void EnterState(IStateSpecificTransitionData transitionData)
        {
            MovementState?.EnterState(transitionData);
            SoundsState?.EnterState(transitionData);
            VisualsState?.EnterState(transitionData);
        }

        public void ExitState()
        {
            MovementState?.ExitState();
            SoundsState?.ExitState();
            VisualsState?.ExitState();
        }

        public void Update(MovementInput input)
        {
            MovementState?.Update(input);
            SoundsState?.Update(input);
            VisualsState?.Update(input);
        }

        public void FixedUpdate()
        {
            MovementState?.FixedUpdate();
            SoundsState?.FixedUpdate();
            VisualsState?.FixedUpdate();
        }
    }
}
