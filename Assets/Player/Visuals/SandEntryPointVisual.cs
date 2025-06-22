using UnityEngine;

public class SandEntryPointVisual : MonoBehaviour
{
    [SerializeField] PlayerController controller;
    private LandMovement landMovement;
    private SpriteRenderer sprite;

    private void Start()
    {
        if (controller == null) return;

        landMovement = controller.StateMachine.GetStateObject<LandMovement>();
        sprite = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (landMovement == null) return;
        //Debug.DrawRay(transform.position, Vector2.up * 5, Color.red);
    }

    private void FixedUpdate()
    {
        if (landMovement == null) return;
        //Debug.DrawRay(transform.position, Vector2.up * 5, Color.green);
        sprite.color = landMovement.SandEntryPosValid ? Color.white : Color.clear;
        transform.position = landMovement.TargetSandEntryPos;
    }
}
