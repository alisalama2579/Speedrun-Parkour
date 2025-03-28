using UnityEngine;

public class UnstableBurrowSand : TraversableTerrain, IWallGrabbable, ISand
{
    private Collider2D col;
    private Color originalColor;
    private bool isFading;
    private float fadedTime;
    public bool IsBurrowable => !isFading && col.enabled;

    protected override void Awake()
    {
        base.Awake();
        col = GetComponent<Collider2D>();
        originalColor = sprite.color;

        EventsHolder.OnPlayerLandOnStableGround += ResetSand;
    }

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
    public void OnSandBurrowEnter(Vector2 _, Vector2 pos) { }

    public void OnSandBurrowExit(Vector2 _, Vector2 pos) { }

    private void ResetSand()
    {
        if (!isFading) return;

        isFading = false;
        col.enabled = true;
        sprite.color = originalColor;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventsHolder.OnPlayerLandOnStableGround -= ResetSand;
    }
}