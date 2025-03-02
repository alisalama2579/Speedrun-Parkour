using UnityEngine;

[CreateAssetMenu(fileName = "PlayerBurrowMovementStats", menuName = "PlayerBurrowMovementStats")]
public class PlayerBurrowMovementStats : ScriptableObject
{
    public LayerMask collisionLayerMask;
    public float collisionDetectionDistance;
    public float speed;
    public float rotationSpeed;

    public float wallBounceStrength;
    public float wallBounceDuration;
}
