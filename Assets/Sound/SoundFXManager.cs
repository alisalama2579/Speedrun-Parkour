using System.Collections;
using UnityEngine;

public class SoundFXManager : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField] private Transform sourceHolder;

    private void PlayClipFromSource(AudioSource source, AudioClip clip, float volume, float pitch)
    {
        source.volume = volume;
        source.pitch = pitch;
        source.clip = clip;

        if(clip == null)
        {
            Destroy(source.gameObject);
            return;
        }

        source.Play();
        Destroy(source.gameObject, clip.length);
    }


    public void PlaySFX(SoundFX soundFX, Transform audioSourceParent)
    {
        if (soundFX == null) 
            return;

        AudioSource clonedSource = Instantiate(source, audioSourceParent);
        PlayClipFromSource(clonedSource, soundFX.clip, soundFX.volume,
             GetPitchFromRange(soundFX.pitchRange));
    }

    public void PlaySFX(SoundFX soundFX, Vector3 position)
    {
        if (soundFX == null)
            return;

        AudioSource clonedSource = Instantiate(source, position, Quaternion.identity);
        PlayClipFromSource(clonedSource, soundFX.clip, soundFX.volume,
             GetPitchFromRange(soundFX.pitchRange));
    }

    public static float GetPitchFromRange(Vector2 pitchRange)
    {
        return Mathf.Clamp01(Random.Range(pitchRange.x, pitchRange.y));
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
}
