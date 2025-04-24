using UnityEngine;

public class BurrowSand : TraversableTerrain, ISand, IWallGrabbable
{
    private Collider2D col;
    protected override void Awake()
    {
        col = GetComponent<Collider2D>();
    }

    public bool IsBurrowable => true;
    public float LaunchSpeed => stats.burrowLaunchSpeed;
    public float WeakLaunchSpeed => stats.burrowWeakLaunchSpeed;
    public void OnSandTargetForBurrow(Vector2 _) { }
    public void OnSandEnter(Vector2 vel, Vector2 pos) { col.isTrigger = true; }    
    public void OnSandExit(Vector2 vel, Vector2 pos) { col.isTrigger = false; }    
}
