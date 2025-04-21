using System.Collections;
using UnityEngine;
using TMPro;

public class RaceStartUI : UIBase
{
    [SerializeField] private uint countdownLength;
    [SerializeField] private float tweenLength;
    [SerializeField] private float pauseLength;
    [SerializeField] private TextMeshProUGUI display;


    public override void Display()
    {
        StartCoroutine(UIAnimation());
    }

    private IEnumerator UIAnimation()
    {
        for (int i = 0; i < countdownLength; i++)
        {
            display.gameObject.SetActive(true);
            display.text = i.ToString();

            for (float t = 0; t < tweenLength; t += Time.deltaTime)
            {
                display.transform.localScale = Vector2.Lerp(Vector2.one, Vector2.one * 5, t / tweenLength);
                yield return null;
            }

            display.gameObject.SetActive(false);

            yield return new WaitForSeconds(2);
        }
    }
}
