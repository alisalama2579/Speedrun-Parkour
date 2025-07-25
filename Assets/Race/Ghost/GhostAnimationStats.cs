using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GhostAnimationStats", menuName = "GhostAnimationStats")]
public class GhostAnimationStats : ScriptableObject
{
    public Animation startAnimation;
    public Animation endAnimation;
    public float startDelay;
}
