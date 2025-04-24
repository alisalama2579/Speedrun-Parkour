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


        if (soundFX is PitchedSoundFX pitchedSoundFX)
            PlayClipFromSource(clonedSource, soundFX.clip, soundFX.volume, Mathf.Clamp01(Random.Range(pitchedSoundFX.pitchRange.x, pitchedSoundFX.pitchRange.y)));
        else
            PlayClipFromSource(clonedSource, soundFX.clip, soundFX.volume, 1);
    }

    public void PlaySFX(SoundFX soundFX, Vector3 position)
    {
        if (soundFX == null)
            return;

        AudioSource clonedSource = Instantiate(source, position, Quaternion.identity);

        if (soundFX is PitchedSoundFX pitchedSoundFX)
             PlayClipFromSource(clonedSource, soundFX.clip, soundFX.volume, Mathf.Clamp01(Random.Range(pitchedSoundFX.pitchRange.x, pitchedSoundFX.pitchRange.y)));
        else
            PlayClipFromSource(clonedSource, soundFX.clip, soundFX.volume, 1);
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
}
