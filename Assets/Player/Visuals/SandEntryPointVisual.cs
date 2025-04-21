using UnityEngine;

public class SandEntryPointVisual : MonoBehaviour
{
    [SerializeField] PlayerController controller;
    private LandMovement landMovement;
    private SpriteRenderer sprite;

    private void Start()
    {
        if (controller == null) return;

        landMovement = controller.StateMachine.GetStateObject(typeof(LandMovement)) as LandMovement;
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (landMovement == null) return;

        sprite.color = landMovement.SandEntryPosValid ? Color.white : Color.clear;
        transform.position = landMovement.TargetSandEntryPos;
    }
}
