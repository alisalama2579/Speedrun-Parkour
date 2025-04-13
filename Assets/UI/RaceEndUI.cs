using System.Collections;
using UnityEngine;

public class RaceEndUI : UIBase
{
    private SpriteRenderer sprite;

    private void Awake() => sprite = GetComponent<SpriteRenderer>();

    public override void StartUI()
    {
        StartCoroutine(UIAnimation());
    }

    private IEnumerator UIAnimation()
    {
        yield return new WaitForSeconds(2);
        sprite.enabled = false;
    }
}
