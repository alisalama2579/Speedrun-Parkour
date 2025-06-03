using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnstablePlatform : TraversableTerrain, IWallGrabbable, IUnstable
{
    [SerializeField] protected TraversalInteractionComponent interactor;
    [SerializeField] protected Collider2D instantFadeDetector;

    protected Collider2D col;
    protected Color originalColor;
    protected bool fadeTriggered;

    private Coroutine fadeRoutine;

    private IEnumerator Fade(float fadeTime, bool disableColImmediate = false)
    {
        fadeTriggered = true;

        float t = 0;
        if (disableColImmediate) col.enabled = false;

        yield return new WaitForSeconds(fadeTime);

        col.enabled = false;

        while (t <= fadeTime)
        {
            sprite.color = Color.Lerp(originalColor, Color.clear, t/fadeTime);
            t += Time.deltaTime;
            yield return null;
        }

        Dissappear();
    }

    private void Dissappear()
    {
        fadeTriggered = true;
        col.enabled = false;
        sprite.color = Color.clear;
    }

    protected void StartFade(float fadeTime, bool disableColImmediate = false)
    {
        StopFade();
        fadeRoutine = StartCoroutine(Fade(fadeTime, disableColImmediate));
    }
    private void StopFade() 
    { 
        if(fadeRoutine != null) StopCoroutine(fadeRoutine);
    } 

    protected virtual void OnPlayerStay(ITerrainInteraction interaction)
    {
        if (!fadeTriggered)
        {
            if (interaction is TerrainInteract)
                StartFade(stats.fadeTime);
            else if (interaction.SurfaceType == TerrainSurfaceType.Ceiling)
                StartFade(stats.fastFadeTime, true);
        }
    }

    protected void ResetSand()
    {
        if (!fadeTriggered) return;

        StopFade();
        fadeTriggered = false;
        col.enabled = true;
        sprite.color = originalColor;
    }

    protected override void Awake()
    {
        base.Awake();
        col = GetComponent<Collider2D>();
        originalColor = sprite.color;

        interactor.OnInteractionStay += OnPlayerStay;
        EventsHolder.PlayerEvents.OnPlayerLandOnGround += OnPlayerLand;
        EventsHolder.PlayerEvents.OnPlayerEnterSand += OnPlayerEnterSand;
    }

    protected virtual void OnPlayerLand(TraversableTerrain terrain)
    {
        if (terrain is not IUnstable) { ResetSand(); }
    }

    protected virtual void OnPlayerEnterSand(ISand sand)
    {
        if (sand is not UnstableBurrowSand) { ResetSand(); }
    }

    protected override void OnDisable()
    {
        base.OnDisable();

        interactor.OnInteractionStay -= OnPlayerStay;
        EventsHolder.PlayerEvents.OnPlayerLandOnGround -= OnPlayerLand;
        EventsHolder.PlayerEvents.OnPlayerEnterSand -= OnPlayerEnterSand;
    }
}