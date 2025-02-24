using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class SandPlatform : WallGrabbableTerrain
{
    private Collider2D col;
    private Color originalColor;
    private bool isFaded;

    protected override void Start() 
    {
        base.Start();
        col = GetComponent<Collider2D>();
        originalColor = sprite.color;

        EventsManager.Instance.OnPlayerLandOnStableGround += ResetSand;
    }

    private IEnumerator FadeSand()
    {
        Color startingColor = sprite.color;
        float t = 0;

        yield return new WaitForSeconds(stats.fadeDelay);
        while (t <= stats.fadeTime)
        {
            t += Time.deltaTime;
            sprite.color = Color.Lerp(startingColor, Color.clear, t / stats.fadeTime);
            yield return null;
        }

        col.enabled = false;
        isFaded = true;
    }

    public override void OnPlayerEnterTerrain(LandMovement controller)
    {
        if (isFaded) return;
        StartCoroutine(FadeSand());
    }

    private void ResetSand()
    {
        if (!isFaded) return;

        StopCoroutine(FadeSand());
        isFaded = false;
        col.enabled = true;
        sprite.color = originalColor;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        EventsManager.Instance.OnPlayerLandOnStableGround -= ResetSand;
    }
}
