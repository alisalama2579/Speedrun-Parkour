using UnityEngine;

public class RaceCamera : MonoBehaviour
{
    [SerializeField] private Rigidbody2D targetRB;
    [SerializeField] public FollowLine line;

    private void FixedUpdate()
    {
        Vector3 targetPosition = line.GetPointOnLevelLine(targetRB.position);
        transform.position = targetPosition + Vector3.forward * transform.position.z;
    }
}
