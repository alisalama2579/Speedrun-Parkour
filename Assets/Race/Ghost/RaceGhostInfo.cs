using UnityEngine;

[CreateAssetMenu(fileName = "RaceGhostInfo", menuName = "ScriptableObjects/Ghost/RaceGhostInfo")]
public class RaceGhostInfo: ScriptableObject
{
    public double raceTime;
    public SpriteRenderer2D sprite;
    public AnimatorType animatorType;


    public enum AnimatorType
    {

    }
}
