using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementProperties", menuName = "PlayerMovementProperties")]
public class PlayerMovementProperties : ScriptableObject
{
    public SurfaceProperty currentSurface;
    public BooleanProperty isOnStableGround;
}
