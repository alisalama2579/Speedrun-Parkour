using UnityEngine;

public class RaceCamera : MonoBehaviour
{
    [SerializeField] private Rigidbody2D targetRB;
    public float smoothTime;
    Vector2 refVel;

    private void FixedUpdate()
    {
        Vector2 targetPosition = Vector2.SmoothDamp(transform.position, targetRB.position, ref refVel, smoothTime);
        transform.position = (Vector3)targetPosition + Vector3.forward * transform.position.z;
    }
}
