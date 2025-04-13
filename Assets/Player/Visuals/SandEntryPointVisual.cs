using UnityEngine;

public class SandEntryPointVisual : MonoBehaviour
{
    [SerializeField] PlayerController controller;
    private LandMovement landMovement;
    private SpriteRenderer sprite;

    private void Start()
    {
        landMovement = controller.MovementMachine.GetStateObject(typeof(LandMovement)) as LandMovement;
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        sprite.color = landMovement.SandEntryPosValid ? Color.white : Color.clear;
        transform.position = landMovement.TargetSandEntryPos;
    }
}
