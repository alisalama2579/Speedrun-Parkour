using System;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class MusicManager : MonoBehaviour
{
    [SerializeField] private SourceTrackPair primarySource;
    [SerializeField] private SourceTrackPair secondarySource;

    public void ReplaceMusic(MusicTrack newTrack, float fadeOutTime, float fadeInTime)
    {
        StopMusic(fadeOutTime);
        StartMusic(newTrack, fadeInTime);
    }
    public void StartMusic(MusicTrack newTrack, float fadeInTime = 1, float targetInVolume = 1, float fadeOutTime = 0, float targetOutVolume = 0)
    {
        if (primarySource == null || secondarySource == null) return;

        Debug.Log("Reached start music");
        ChangeSourceMusic(secondarySource, primarySource.track, targetOutVolume, fadeOutTime);
        ChangeSourceMusic(primarySource, newTrack, targetInVolume, fadeInTime);
    }

    public void StopMusic(float fadeOutTime)
    {
        Debug.Log("Reached stop music");
        if (primarySource.track) primarySource.track.FadeOut(fadeOutTime);
        if (secondarySource.track) secondarySource.track.FadeOut(fadeOutTime);
    }

    private void ChangeSourceMusic(SourceTrackPair sourceInfoPair, MusicTrack track, float targetVolume, float fadeTime)
    {
        if (track == null) return;

        Debug.Log("Reached change source music");
        track.SetMasterFade(targetVolume, fadeTime);
        sourceInfoPair.ChangeTrack(track);
    }

    private void Update()
    {
        primarySource.UpdateVolume();
        secondarySource.UpdateVolume();
    }

    [Serializable]
    private class SourceTrackPair
    {
        public AudioSource source;
        public AudioSource[] layers;
        [HideInInspector] public float masterVolume;
        [HideInInspector] public MusicTrack track;

        public void ChangeTrack(MusicTrack newTrack)
        {
            Debug.Log("Reached change track to " + newTrack);

            track = newTrack;
            if (newTrack == null) { source.Stop(); source.clip = null; return; }

            newTrack.OnStart();
            source.clip = track.info.clip;
            source.loop = track.info.loop;
            source.Play();

            for (int i = 0; i < track.layers.Length; i++)
            {
                AudioSource source = layers[i];
                MusicInfo info = track.layers[i].info;

                source.clip = info.clip;
                source.loop = info.loop;
                source.Play();
            }
        }
        public void UpdateVolume()
        {
            if(track  == null) return;
            if (!track.isActive) { ChangeTrack(null); return; }


            Debug.Log("Reached update track ");
            track.UpdateMusic();

            float trackVolume = track.volume;
            source.volume = trackVolume;

            for(int i = 0; i < track.layers.Length; i++)
            {
                AudioSource source = layers[i];
                source.volume = track.layers[i].volume * trackVolume;
            }
        }
    }
}

[Serializable]
public class MusicInfo
{
    public float MaxVolume => maxVolume;

    [SerializeField] private float maxVolume;
    public AudioClip clip;
    public bool loop;
}