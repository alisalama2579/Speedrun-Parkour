using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Player : MonoBehaviour
{
    public PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Enable();
    }

    private void HandleInput()
    {
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
