using UnityEngine;

public struct VisualsInitData
{
    public Transform Transform;
    public Animator Anim;
    public SpriteRenderer Renderer;
    public AnimationStatsHolder Stats;
}

public struct SoundInitData
{
    public Transform Transform;
    public SoundFXManager SoundFXManager;
    public PlayerSoundStats Stats;
}

public struct MovementInitData
{
    public Transform Transform;
    public Rigidbody2D RB;
    public Collider2D Col;
    public MovementStatsHolder Stats;
    public PlayerMovementProperties Properties;
}
