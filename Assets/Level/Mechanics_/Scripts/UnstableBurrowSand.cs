using System;
using UnityEngine;

public class UnstableBurrowSand : TraversableTerrain, IWallGrabbable, ISand, IUnstable
{
    private Collider2D col;
    private Color originalColor;
    private bool isFading;
    private float fadedTime;
    public bool IsBurrowable => !isFading && col.enabled;
    public float LaunchSpeed => stats.sandLaunchSpeed;
    public float WeakLaunchSpeed => LaunchSpeed;

    private void Update()
    {
        if (isFading)
        {
            fadedTime += Time.deltaTime;

            if (fadedTime <= stats.fadeDelay + stats.fadeTime)
            {
                if (fadedTime > stats.fadeDelay) sprite.color = Color.Lerp(originalColor, Color.clear, (fadedTime - stats.fadeDelay) / stats.fadeTime);
            }
            else 
            {
                col.enabled = false;
                sprite.color = Color.clear; 
            }
        }
        else fadedTime = 0;
    }

    public override void OnEnterTerrain() => isFading = true;
    public void OnSandTargetForBurrow(Vector2 _)
    {
        col.enabled = false;
        isFading = true;
    }

    private void ResetSand()
    {
        if (!isFading) return;

        isFading = false;
        col.enabled = true;
        sprite.color = originalColor;
    }


    protected override void Awake()
    {
        base.Awake();
        col = GetComponent<Collider2D>();
        originalColor = sprite.color;
        EventsHolder.PlayerEvents.OnPlayerLandOnGround += OnPlayerLand;
        EventsHolder.PlayerEvents.OnPlayerBurrow += OnPlayerBurrow;
    }

    private void OnPlayerLand(TraversableTerrain terrain) 
    {
        if (terrain is not IUnstable) ResetSand();
    }

    private void OnPlayerBurrow(ISand sand)
    {
        if (sand is not UnstableBurrowSand) ResetSand();
    }


    protected override void OnDisable()
    {
        base.OnDisable();

        EventsHolder.PlayerEvents.OnPlayerLandOnGround -= OnPlayerLand;
        EventsHolder.PlayerEvents.OnPlayerBurrow -= OnPlayerBurrow;
    }

    public void OnSandBurrowExit(Vector2 _, Vector2 pos) { }
    public void OnSandBurrowEnter(Vector2 _, Vector2 pos) { }
}