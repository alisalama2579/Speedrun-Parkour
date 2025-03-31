using UnityEngine;
public class SandBall : TraversableTerrain, ISand
{
    private Collider2D col;
    protected override void Awake()
    {
        base.Awake();
        col = GetComponent<Collider2D>();
    }

    public bool IsBurrowable => true;
    public void OnSandTargetForBurrow(Vector2 _) { col.enabled = false; }
    public void OnSandBurrowExit(Vector2 vel, Vector2 pos) { col.enabled = true; }
}
