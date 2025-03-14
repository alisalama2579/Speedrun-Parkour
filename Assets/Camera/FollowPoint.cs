using UnityEngine;

public class FollowPoint : MonoBehaviour
{
    [HideInInspector] public Vector2 position;
    public float nextPointDistance;
    public Vector2 nextPointDir;

    private void Awake()
    {
        position = transform.position;
        Debug.DrawRay(position, transform.up, Color.yellow, 10);
    }
}
