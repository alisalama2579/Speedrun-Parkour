using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField] private Transform sourceHolder;

    [SerializeField] private SoundFXGroup[] groups;

    private void Awake()
    {
        for (int i = 0; i < groups.Length; i++)
            groups[i].Initialize();
    }

    private void PlayClipFromSource(AudioSource source, AudioClip clip, float volume, float pitch)
    {
        if (source == null)
            return;

        source.volume = volume;
        source.pitch = pitch;
        source.clip = clip;

        if (clip == null)
            return;

        source.Stop();
        source.Play();
    }


    public void PlaySFX(SoundFX soundFX, Transform audioSourceParent)
    {
        if (soundFX == null)
            return;

        AudioSource clonedSource = InstantiateAudio(audioSourceParent, soundFX.type);
        PlayClipFromSource(clonedSource, soundFX.clip, soundFX.volume,
             GetPitchFromRange(soundFX.pitchRange));
    }

    public void PlaySFX(SoundFX soundFX, Vector3 position)
    {
        if (soundFX == null)
            return;

        AudioSource clonedSource = InstantiateAudio(position, Quaternion.identity, soundFX.type);
        PlayClipFromSource(clonedSource, soundFX.clip, soundFX.volume,
             GetPitchFromRange(soundFX.pitchRange));
    }

    public static float GetPitchFromRange(Vector2 pitchRange)
    {
        return Mathf.Clamp01(UnityEngine.Random.Range(pitchRange.x, pitchRange.y));
    }

    /// <summary>
    ///  Intended for looping audios where more class-specific control is required for stopping/starting, pitch etc.
    /// </summary>
    public AudioSource GetLoopingSFX(Vector3 position)
    {
        AudioSource clonedSource = Instantiate(source, position, Quaternion.identity, sourceHolder);
        clonedSource.loop = true;

        return clonedSource;
    }

    public AudioSource GetLoopingSFX(Transform parentTransform)
    {
        AudioSource clonedSource = Instantiate(source, parentTransform);
        clonedSource.loop = true;

        return clonedSource;
    }

    public static void ChangeSourceSound(AudioSource source, SoundFX newSound)
    {
        source.clip = newSound.clip;
        source.volume = newSound.volume;
        source.pitch = GetPitchFromRange(newSound.pitchRange);
    }


    private AudioSource InstantiateAudio(Transform transform, SoundFXType sfxType)
    {
        Queue<AudioSource> sources = groups[(int)sfxType]?.sources;
        if (sources == null) return null;

        AudioSource clonedSource = sources.Dequeue();
        sources.Enqueue(clonedSource);
        return clonedSource;
    }
    private AudioSource InstantiateAudio(Vector2 pos, Quaternion rot, SoundFXType sfxType)
    {
        Queue<AudioSource> sources = groups[(int)sfxType]?.sources;
        if (sources == null) return null;

        AudioSource clonedSource = sources.Dequeue();
        clonedSource.transform.SetPositionAndRotation(pos, rot);
        sources.Enqueue(clonedSource);

        return clonedSource;
    }

    public enum SoundFXType
    {
        PlayerMovement,
        PlayerOther,
        GameSound,
        AmbientSound
    }

    [Serializable]
    public class SoundFXGroup
    {
        [SerializeField] private int sfxCount;
        [SerializeField] private Transform sourceHolder;
        [SerializeField] private AudioSource source;
        [HideInInspector] public Queue<AudioSource> sources = new();

        public void Initialize()
        {
            for (int i = 0; i < sfxCount; i++)
            {
                sources.Enqueue(Instantiate(source, sourceHolder));
            }
        }
    }
}