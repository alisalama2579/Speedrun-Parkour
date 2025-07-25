using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "MusicTrack", menuName = "ScriptableObjects/Music/MusicTrack")]
public class MusicTrack : ScriptableObject
{
    public MusicInfo info;
    private float channelVolume;
    private float targetChannelVolume;
    private float masterVolume;
    private float targetMasterVolume;

    [HideInInspector] public float volume;

    private float channelFadeSpeed;
    private float masterFadeSpeed;
    [HideInInspector] public bool isActive;
    private bool isFadingOut;

    public MusicLayer[] layers = new MusicLayer[0];
    private void OnValidate()
    {
        channelVolume = info.MaxVolume;
        targetChannelVolume = info.MaxVolume;
    }
    public void OnStart()
    {
        channelVolume = info.MaxVolume;
        targetChannelVolume = info.MaxVolume;
        isActive = true;
        isFadingOut = false;
    }
    private void OnStop()
    {
        channelVolume = info.MaxVolume;
        targetChannelVolume = info.MaxVolume;
        isActive = false;
        isFadingOut = false;
    }
    public void FadeOut(float fadeTime = 0)
    {
        SetMasterFade(0, fadeTime);
        isFadingOut = true;
    }


    public void SetFade(float targetVolume, float fadeTime = 0)
    {
        targetChannelVolume = Mathf.Clamp01(targetVolume) * info.MaxVolume;

        if (fadeTime == 0) channelFadeSpeed = float.MaxValue;
        else channelFadeSpeed = Mathf.Abs((targetChannelVolume - channelVolume) / fadeTime);
    }
    public void SetMasterFade(float targetVolume, float fadeTime = 0)
    {
        targetMasterVolume = Mathf.Clamp01(targetVolume) * info.MaxVolume;

        if (fadeTime == 0) masterFadeSpeed = float.MaxValue;
        else masterFadeSpeed = Mathf.Abs((targetMasterVolume - masterVolume)/fadeTime);
    }

    public void UpdateMusic()
    {
        if (Mathf.Approximately(masterVolume, 0) && isFadingOut) isActive = false;

        if (!isActive) { OnStop(); return; }

        channelVolume = Mathf.Clamp01(Mathf.MoveTowards(channelVolume, targetChannelVolume, channelFadeSpeed * Time.deltaTime));
        masterVolume = Mathf.Clamp01(Mathf.MoveTowards(masterVolume, targetMasterVolume, masterFadeSpeed * Time.deltaTime));

        volume = channelVolume * masterVolume;


        for (int i = 0; i < layers.Length; i++) 
        {
            MusicLayer layer = layers[i];
            layer.UpdateLayer(); 
        }
    }
}
