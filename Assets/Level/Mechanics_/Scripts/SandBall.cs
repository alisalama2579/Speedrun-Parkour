using System.Collections;
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
    public float LaunchSpeed => stats.sandLaunchSpeed;
    public float WeakLaunchSpeed => LaunchSpeed;

    public void OnSandTargetForBurrow(Vector2 _) { col.enabled = false; StopCoroutine(EnableCollider()); }
    public void OnSandEnter(Vector2 vel, Vector2 pos) {  }
    public void OnSandExit(Vector2 vel, Vector2 pos) { StartCoroutine(EnableCollider()); }


    private IEnumerator EnableCollider()
    {
        yield return new WaitForSeconds(stats.sandColliderReactivationDelay);
        col.enabled = true;
    }
}
