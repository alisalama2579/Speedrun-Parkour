using System;
using System.Collections.Generic;
using UnityEngine;
using static TransitionLibrary;

public class MovementStateMachine
{
    public void AddTransition(Type from, Type to, Func<IStateSpecificTransitionData>  func)
        => GetNode(from).AddTransition(GetNode(to).State, func);
    public void AddUnconditionalTransition(Type from, Type to)
        => GetNode(from).AddTransition(GetNode(to).State, AnyTransitionFunc);
    public void AddAnyTransition(Type to) 
        => anyTransitions.Add(new Transition(GetNode(to).State, AnyTransitionFunc));


    public IState GetStateObject(Type type) => GetNode(type).State;
    StateNode GetNode(Type type) => nodes.GetValueOrDefault(type);
    void AddNode(Type type, IState state) => nodes.Add(type, new StateNode(state));

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

    public MovementStateMachine(Type startingType, MovementStatsHolder statsHolder, Rigidbody2D rb, Collider2D col)
    {
        AddNode(typeof(LandMovement), new LandMovement(rb, col, statsHolder));
        AddNode(typeof(BurrowMovement), new BurrowMovement(rb, col, statsHolder));
        AddNode(typeof(SandEntryMovement), new SandEntryMovement(rb, col, statsHolder));
        InitializeStateTransitions();

        SetStartingState(startingType);
    }

    private void InitializeStateTransitions()
    {
        foreach (StateNode stateNode in nodes.Values) stateNode.State.InitializeTransitions(this);
    }

    private void SetStartingState(Type startingType)
    {
        current = GetNode(startingType);
        current.State?.EnterState(failedData);
    }

    private void SwitchMovementState(IState state, IStateSpecificTransitionData transitionData) 
    {
        if (state == current.State) return;

        current.State?.ExitState();
        current = GetNode(state.GetType());
        current.State?.EnterState(transitionData);
    }

    public void Update(Player.Input frameInput)
    {
        var transition = GetTransition(out IStateSpecificTransitionData transitionData);
        if (transition != null) SwitchMovementState(transition.To, transitionData);
        current.State?.Update(frameInput);
    }

    public void FixedUpdate() 
    {
        current.State?.UpdateMovement();
    }

    public void CollisionEnter(Collision2D collision) => current.State?.CollisionEnter(collision);
    public void CollisionExit(Collision2D collision) => current.State?.CollisionExit(collision);
    public void TriggerEnter(Collider2D trigger) => current.State?.TriggerEnter(trigger); 
    public void TriggerExit(Collider2D trigger) => current.State?.TriggerExit(trigger); 

    public void CollisionEnter(IPlayerCollisionListener collisionListener) => current.State?.CollisionEnter(collisionListener);
    public void CollisionExit(IPlayerCollisionListener collisionListener) => current.State?.CollisionExit(collisionListener);
    public void TriggerEnter(IPlayerCollisionListener collisionListener) => current.State?.TriggerEnter(collisionListener);
    public void TriggerExit(IPlayerCollisionListener collisionListener) => current.State?.TriggerExit(collisionListener);


    class StateNode
    {
        public IState State { get; }
        public HashSet<Transition> Transitions { get; }

        public StateNode(IState state)
        {
            State = state;
            Transitions = new HashSet<Transition>();
        }

        public void AddTransition(IState state, Func<IStateSpecificTransitionData> func) => Transitions.Add(new Transition(state, func));
    }

}
