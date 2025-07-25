using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MusicLayer", menuName =  "ScriptableObjects/Music/MusicLayer")]
public class MusicLayer : ScriptableObject
{
    public MusicInfo info;
    public BooleanProperty isActivatedProperty;

    private bool isActivated;
    private float childVolume;
    [HideInInspector] public float volume;
    [HideInInspector] public float fadeOutSpeed;

    private float targetVolume;
    private float fadeSpeed;

    private void OnValidate()
    {
        volume = info.MaxVolume;
        targetVolume = info.MaxVolume;
    }

    public void SetFade(float targetVolumePercent, float fadeTime = 0)
    {
        targetVolume = Mathf.Clamp01(targetVolumePercent);

        if (fadeTime == 0) fadeSpeed = float.MaxValue;
        else fadeSpeed = Mathf.Abs((targetVolume - volume) / fadeTime);
    }

    public void UpdateLayer()
    {
        isActivated = isActivatedProperty.Value;

        if (!isActivated) SetFade(0, fadeOutSpeed);
        else SetFade(info.MaxVolume, fadeOutSpeed);

        childVolume = Mathf.Clamp01(Mathf.MoveTowards(childVolume, targetVolume, fadeSpeed * Time.deltaTime));
        volume = childVolume;
    }
}
