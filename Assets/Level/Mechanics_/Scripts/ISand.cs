using UnityEngine;

public interface ISand
{
    public bool IsBurrowable { get; }
    public void OnSandTargetForBurrow(Vector2 vel);
    public void OnSandBurrowExit(Vector2 vel, Vector2 pos);
    public void OnSandBurrowEnter(Vector2 vel, Vector2 pos)
    {
    }
}
