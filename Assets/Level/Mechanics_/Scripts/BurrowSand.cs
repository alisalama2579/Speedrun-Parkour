using UnityEngine;

public class BurrowSand : TraversableTerrain, ISand, IWallGrabbable
{
    public bool IsBurrowable => true;
    public float LaunchSpeed => stats.burrowLaunchSpeed;
    public float WeakLaunchSpeed => stats.burrowWeakLaunchSpeed;
    public void OnSandTargetForBurrow(Vector2 _) { }
    public void OnSandBurrowExit(Vector2 vel, Vector2 pos)    { }    
    public void OnSandBurrowEnter(Vector2 vel, Vector2 pos)    { }    
}
