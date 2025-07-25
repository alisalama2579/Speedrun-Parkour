using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementProperties", menuName = "ScriptableObjects/Player/PlayerMovementProperties")]
public class PlayerMovementProperties : ScriptableObject
{
    public SurfaceProperty currentSurface;
    public BooleanProperty isOnStableGround;
}
