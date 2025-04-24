using UnityEngine;
using UnityEngine.Serialization;

public class SpringTest : MonoBehaviour
{
    public float equilibrium;
    public float angularFrequency;
    public float dampingRatio;

    private float refPos;
    private float refVel;

    private SpringUtils.DampedSpringMotionParams motionParams = new();

    private void FixedUpdate()
    {
        SpringUtils.CalcDampedSpringMotionParams(motionParams, Time.deltaTime, angularFrequency, dampingRatio);
        SpringUtils.UpdateDampedSpringMotion(ref refPos, ref refVel, equilibrium, motionParams);

        transform.position = new Vector2(refPos, transform.position.y);
    }
}
