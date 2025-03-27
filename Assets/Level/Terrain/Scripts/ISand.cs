using UnityEngine;

public interface ISand
{
    public bool IsBurrowable { get; }
    public void OnSandTargetForBurrow(Vector2 entryVel);
    public void OnSandBurrowExit(Vector2 exitVel);
    public void OnSandBurrowEnter(Vector2 entryVel);
}
