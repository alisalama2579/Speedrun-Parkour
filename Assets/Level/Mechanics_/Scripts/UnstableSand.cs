using UnityEngine;

public class UnstableSand : TraversableTerrain, IWallGrabbable, ISand, IUnstable
{
    private Collider2D col;
    private Color originalColor;
    private bool isFading;
    private float fadedTime;
    public bool IsBurrowable => false;

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
    public void OnSandTargetForBurrow(Vector2 _) { }
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