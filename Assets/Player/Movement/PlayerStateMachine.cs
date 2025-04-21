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
    public void AddAnyTransition(Type to) 
        => anyTransitions.Add(new Transition(GetNode(to).MovementState, AnyTransitionFunc));


    public IState GetStateObject(Type type) => GetNode(type).MovementState;
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

    public PlayerStateMachine(Type startingType, Transform transform, Rigidbody2D rb, Collider2D col, MovementStatsHolder movementStats, AnimationStatsHolder animationStats, Animator anim, PlayerSoundStats soundStats, SoundFXManager sfxManager)
    {
        LandMovement landMovement = new LandMovement(rb, col, movementStats);
        AddNode(typeof(LandMovement),
            landMovement,
            new LandVisuals(landMovement, transform, animationStats, anim), 
            new LandSound(landMovement, transform, soundStats, sfxManager));

        BurrowMovement burrowMovement = new BurrowMovement(rb, col, movementStats);
        AddNode(typeof(BurrowMovement), 
            burrowMovement, 
            new BurrowVisuals(burrowMovement, transform, animationStats, anim), 
            new BurrowSound(burrowMovement, transform, soundStats, sfxManager));

        SandEntryMovement sandEntryMovement = new SandEntryMovement(rb, col, movementStats);
        AddNode(typeof(SandEntryMovement), 
            new SandEntryMovement(rb, col, movementStats), 
            new SandEntryVisuals(sandEntryMovement, transform, animationStats, anim),
            null);

        InitializeStateTransitions();
        SetStartingState(startingType);
    }

    private void InitializeStateTransitions() { foreach (StateNode stateNode in nodes.Values) stateNode.MovementState.InitializeTransitions(this); }
    private void SetStartingState(Type startingType)
    {
        current = GetNode(startingType);
        current.EnterState(failedData);
    }

    private void SwitchMovementState(IState state, IStateSpecificTransitionData transitionData) 
    {
        if (state == current.MovementState) return;

        current.ExitState();

        current = GetNode(state.GetType());

        current.EnterState(transitionData);
    }

    public void Update(Player.Input frameInput)
    {
        current.Update(frameInput);

        var transition = GetTransition(out IStateSpecificTransitionData transitionData);
        if (transition != null) SwitchMovementState(transition.To, transitionData);
    }

    public void FixedUpdate() 
    {
        current.FixedUpdate();
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

    public void CollisionEnter(IPlayerCollisionListener collisionListener) => current.MovementState?.CollisionEnter(collisionListener);
    public void CollisionExit(IPlayerCollisionListener collisionListener) => current.MovementState?.CollisionExit(collisionListener);
    public void TriggerEnter(IPlayerCollisionListener collisionListener) => current.MovementState?.TriggerEnter(collisionListener);
    public void TriggerExit(IPlayerCollisionListener collisionListener) => current.MovementState?.TriggerExit(collisionListener);

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

        public void Update(Player.Input frameInput)
        {
            MovementState?.Update(frameInput);
            SoundsState?.Update(frameInput);
            VisualsState?.Update(frameInput);
        }

        public void FixedUpdate()
        {
            MovementState?.FixedUpdate();
            SoundsState?.FixedUpdate();
            VisualsState?.FixedUpdate();
        }
    }

    public class StateTransitionEvent
    {
        public event Action<Type> transitionEvent;

        public StateTransitionEvent(Action<Type> transitionEvent)
        {
            this.transitionEvent = transitionEvent;
        }
    }
}
