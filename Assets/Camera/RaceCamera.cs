using UnityEngine;
using UnityEngine.Serialization;

public class RaceCamera : MonoBehaviour
{
    [SerializeField] private Rigidbody2D targetRB;

    [SerializeField] private float smoothTime;
    Vector2 refVel;

    [FormerlySerializedAs("maxDisplacement")]
    [SerializeField] private Vector2 deadZone;
    [SerializeField] private float maximumMoveSpeed;
    [SerializeField] private float distanceToSnap;

    private void FixedUpdate()
    {
        Vector2 targetPosition = targetRB.position;
        Vector2 smoothedPosition = Vector2.SmoothDamp(transform.position, targetPosition, ref refVel, smoothTime);

        float xDisplacement =  smoothedPosition.x - targetPosition.x;
        xDisplacement = Mathf.Clamp(xDisplacement, -deadZone.x, deadZone.x);

        float yDisplacement =  smoothedPosition.y - targetPosition.y;
        yDisplacement = Mathf.Clamp(yDisplacement, -deadZone.y, deadZone.y);

        Vector2 deadzonedPosition = new Vector2(targetPosition.x + xDisplacement, targetPosition.y + yDisplacement);

        float xDelta = deadzonedPosition.x - transform.position.x;
        float yDelta = deadzonedPosition.y - transform.position.y;

        Vector2 finalPosition;

        if (new Vector2(xDelta, yDelta).sqrMagnitude >= distanceToSnap)
            finalPosition = targetPosition;
        else
        {
            float clampedXDelta = Mathf.Clamp(xDelta, -maximumMoveSpeed, maximumMoveSpeed);
            float clampedYDelta = Mathf.Clamp(yDelta, -maximumMoveSpeed, maximumMoveSpeed);

            finalPosition = new Vector2(transform.position.x + clampedXDelta, transform.position.y + clampedYDelta);
        }

        transform.position = new Vector3(finalPosition.x, finalPosition.y, transform.position.z);
    }

}
