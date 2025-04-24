using UnityEngine;
using UnityEngine.Serialization;

public class RaceCamera : MonoBehaviour
{
    [SerializeField] private Rigidbody2D targetRB;

    [FormerlySerializedAs("maxDisplacement")]
    [SerializeField] private Vector2 deadZone;
    [SerializeField] private float maximumMoveSpeed;
    [SerializeField] private float distanceToSnap;

    private Vector2 equilibrium;
    public float dampingRatio;
    public float frequency;

    private Vector2 refPos;
    private Vector2 refVel;

    private SpringUtils.DampedSpringMotionParams motionParams = new();

    private void FixedUpdate()
    {
        Vector2 targetPosition = equilibrium = targetRB.position;

        SpringUtils.CalcDampedSpringMotionParams(motionParams, Time.deltaTime, frequency, dampingRatio);
        SpringUtils.UpdateDampedSpringMotion(ref refPos.x, ref refVel.x, equilibrium.x, motionParams);
        SpringUtils.UpdateDampedSpringMotion(ref refPos.y, ref refVel.y, equilibrium.y, motionParams);
        Vector2 springedPos = refPos;

        float xDisplacement =  springedPos.x - targetPosition.x;
        xDisplacement = Mathf.Clamp(xDisplacement, -deadZone.x, deadZone.x);

        float yDisplacement =  springedPos.y - targetPosition.y;
        yDisplacement = Mathf.Clamp(yDisplacement, -deadZone.y, deadZone.y);

        Vector2 deadzonedPosition = new Vector2(targetPosition.x + xDisplacement, targetPosition.y + yDisplacement);

        float xDelta = deadzonedPosition.x - transform.position.x;
        float yDelta = deadzonedPosition.y - transform.position.y;

        Vector2 finalPosition;

        if (new Vector2(xDelta, yDelta).sqrMagnitude >= distanceToSnap * distanceToSnap)
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
