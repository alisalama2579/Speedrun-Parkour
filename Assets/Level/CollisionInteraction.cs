using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public interface IPlayerCollisionInteractor
{
}

public interface IPlayerCollisionListener : IPlayerCollisionInteractor
{
    public void OnPlayerEnter() { }
    public void OnPlayerExit() { }
    public void OnPlayerStay() { }
}

public interface IInteractionProvider
{
    public event Action<IPlayerCollisionInteractor> OnCollisionInteraction;
}
public enum CollisionType
{
    Stayed,
    Entered,
    Exited
}




