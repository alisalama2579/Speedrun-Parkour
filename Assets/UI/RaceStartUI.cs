using System.Collections;
using UnityEngine;

public class RaceStartUI : UIBase
{
    private SpriteRenderer sprite;

    private void Awake() => sprite = GetComponent<SpriteRenderer>();

    public override void StartUI()
    {
        sprite.enabled = false;
        StartCoroutine(UIAnimation());
    }

    private IEnumerator UIAnimation()
    {
        yield return new WaitForSeconds(2);
        sprite.enabled = true;
    }
}
