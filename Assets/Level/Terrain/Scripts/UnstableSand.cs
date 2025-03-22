using UnityEngine;

public class UnstableSand : TraversableTerrain, IWallGrabbable, ISand
{
    private Collider2D col;
    private Color originalColor;
    private bool isFading;
    private float fadedTime;

    protected override void Awake()
    {
        base.Awake();
        col = GetComponent<Collider2D>();
        originalColor = sprite.color;

        EventsManager.Instance.OnPlayerLandOnStableGround += ResetSand;
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
            else col.enabled = false;
        }
        else fadedTime = 0;
    }

    public override void OnPlayerEnterTerrain(LandMovement controller) => isFading = true;
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
        EventsManager.Instance.OnPlayerLandOnStableGround -= ResetSand;
    }
}