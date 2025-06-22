using UnityEngine;

[CreateAssetMenu(fileName = "PlayerProperties", menuName = "PlayerProperties")]
public class PlayerProperties : ScriptableObject
{
    public BooleanProperty enteredRaceStartPosition;
    public Vector3Property lastStablePosition;
    public PlayerMovementProperties movementProperties;
}
