using UnityEngine;

public interface ISand
{
    public bool IsBurrowable { get; }
    public float LaunchSpeed { get; }
    public float WeakLaunchSpeed { get; }
    public void OnSandTargetForBurrow(Vector2 vel);
    public void OnSandEnter(Vector2 vel, Vector2 pos);
    public void OnSandExit(Vector2 vel, Vector2 pos);
}
