using System;
using UnityEngine;
using UnityEngine.InputSystem;

public struct MovementInput
{
    public Vector2 Look;
    public Vector2 NonZeroLook;
    public Vector2 SnappedLook;
    public float HorizontalMove;
    public float NonZeroHorizontalMove;
    public float SnappedHorizontalMove;

    public bool DashDown;
    public bool DashHeld;

    public bool SandDashDown;
    public bool SandDashHeld;

    public bool JumpDown;
    public bool JumpHeld;

    public void Update(PlayerControls input)
    {
        Look = input.PlayerMovement.Look.ReadValue<Vector2>();
        SnappedLook = Snap(Look);
        if (Look != Vector2.zero) NonZeroLook = Look;

        HorizontalMove = input.PlayerMovement.HorizontalMove.ReadValue<float>();
        SnappedHorizontalMove = Snap(HorizontalMove);
        if (HorizontalMove != 0) NonZeroHorizontalMove = HorizontalMove;

        DashDown = input.PlayerMovement.Dash.WasPressedThisFrame();
        DashHeld = input.PlayerMovement.Dash.IsPressed();

        SandDashDown = input.PlayerMovement.SandDash.WasPressedThisFrame();
        SandDashHeld = input.PlayerMovement.SandDash.IsPressed();

        JumpDown = input.PlayerMovement.Jump.WasPressedThisFrame();
        JumpHeld = input.PlayerMovement.Jump.IsPressed();
    }

    private readonly Vector2 Snap(Vector2 v) => v == Vector2.zero ? v : new Vector2(Mathf.Sign(v.x), Mathf.Sign(v.y));
    private readonly float Snap(float f) => f == 0 ? f : Mathf.Sign(f);
}
