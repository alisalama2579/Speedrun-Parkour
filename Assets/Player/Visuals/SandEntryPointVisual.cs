using UnityEngine;

public class SandEntryPointVisual : MonoBehaviour
{
    [SerializeField] Player player;
    private LandMovement landMovement;
    private SpriteRenderer sprite;

    private void Start()
    {
        landMovement = player.MovementMachine.GetStateObject(typeof(LandMovement)) as LandMovement;
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        sprite.color = landMovement.SandEntryPosValid ? Color.clear : Color.white;
        transform.position = landMovement.TargetSandEntryPos;
    }
}
