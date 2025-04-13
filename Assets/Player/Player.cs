using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    private PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Enable();
    }

    public struct Input
    {
        public bool DashDown;
        public bool SandDashDown;
        public bool SandDashHeld;
        public bool JumpPressed;
        public bool JumpHeld;
        public float HorizontalMove;
        public Vector2 Move;
    }
    public Input FrameInput => frameInput;
    private Input frameInput;

    private void HandleInput()
    {
        frameInput = new Input
        {
            JumpPressed = controls.PlayerMovement.Jump.WasPressedThisFrame(),
            JumpHeld = controls.PlayerMovement.Jump.IsPressed(),
            HorizontalMove = controls.PlayerMovement.HorizontalMove.ReadValue<float>(),
            DashDown = controls.PlayerMovement.Dash.WasPressedThisFrame(),
            SandDashDown = controls.PlayerMovement.SandDash.WasPressedThisFrame(),
            SandDashHeld = controls.PlayerMovement.SandDash.IsPressed(),
            Move = controls.PlayerMovement.Move.ReadValue<Vector2>()
        };

        if (frameInput.HorizontalMove != 0) frameInput.HorizontalMove = Mathf.Sign(frameInput.HorizontalMove);
    }

    private void OnDisable()
    {
        controls.Disable();
        controls = null;
    }

    private void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
    }

    public void OnDeath()
    {
        EventsHolder.PlayerEvents.OnPlayerDeath?.Invoke();
    }
}
