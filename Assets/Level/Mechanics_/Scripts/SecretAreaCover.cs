using UnityEngine;

public class SecretAreaCover : MonoBehaviour, IPlayerCollisionListener
{
    [SerializeField] private LevelMechanicStats stats;
    private SpriteRenderer sprite;
    private Color originalColor;

    private float fadedTime;
    private bool isFading;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        originalColor = sprite.color;
    }
    public void OnPlayerEnter()
    {
        sprite.color = Color.clear;
        isFading = false;
    }

    private void Update()
    {
        if (isFading)
        {
            fadedTime += Time.deltaTime;

            if (fadedTime <= stats.secretFadeTime)
                sprite.color = Color.Lerp(Color.clear, originalColor, fadedTime/stats.secretFadeTime);
            else
                sprite.color = originalColor;
        }
        else fadedTime = 0;
    }


    public void OnPlayerExit()
    {
        isFading = true;
    }

    public void OnPlayerStay() { }
}
