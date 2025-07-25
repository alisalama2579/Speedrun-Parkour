using System;
using System.Collections.Generic;
using UnityEngine;
using static TransitionLibrary;

public interface IStateMachine
{
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
        if(AnyTransitions != null)
        {
            foreach (var transition in AnyTransitions)
            {
                if (transition.TryExecute(out transitionData))
                    return transition;
            }
        }

        foreach (var transition in Current.Transitions)
        {
            if (transition.TryExecute(out transitionData))
                return transition;
        }

        transitionData = failedData;
        return null;
    }


    public StateNode Current { get; set; }
    public Dictionary<Type, StateNode> Nodes { get; set; }
    public HashSet<Transition> AnyTransitions { get; set; }

    public void Initialize(Type startingType)
    {
        SetStartingState(startingType);
    }

    public void InitializeTransitions()
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

}



public class StateNode : IState
{
    public IState State { get; }
    public HashSet<Transition> Transitions { get; }

    public StateNode(IState state)
    {
        Transitions = new HashSet<Transition>();

        State = state;
    }

    public void AddTransition(IState to, Func<IStateSpecificTransitionData> func)
    {
        Transitions.Add(new Transition(to, func));
    }

    public void EnterState(IStateSpecificTransitionData transitionData)
    {
        State?.EnterState(transitionData);
    }

    public void ExitState()
    {
        State?.ExitState();
    }

    public void UpdateState()
    {
        State?.UpdateState();
    }

    public void FixedUpdate()
    {
        State?.FixedUpdate();
    }
}
