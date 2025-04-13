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

        source.Play();
        Destroy(source, clip.length);
    }


    public void HandleSoundPlaying(SoundInfo info, Transform audioSourceParent)
    {
        AudioSource clonedSource = Instantiate(source, audioSourceParent);

        if (info.pitchRange != Vector2.zero)
            PlayClipFromSource(clonedSource, info.clip, info.volume, Mathf.Clamp01(UnityEngine.Random.Range(info.pitchRange.x, info.pitchRange.y)));
        else
            PlayClipFromSource(clonedSource, info.clip, info.volume, 1);
    }

    public void PlaySFX(SoundInfo info, Vector3 position)
    {
        AudioSource clonedSource = Instantiate(source, position, Quaternion.identity);

        if (info.pitchRange != Vector2.zero)
             PlayClipFromSource(clonedSource, info.clip, info.volume, Mathf.Clamp01(UnityEngine.Random.Range(info.pitchRange.x, info.pitchRange.y)));
        else
            PlayClipFromSource(clonedSource, info.clip, info.volume, 1);
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
