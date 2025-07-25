using System.Collections;
using UnityEngine;

public class RaceMusicController : MonoBehaviour
{
    [SerializeField] private MusicManager musicManager;

    [SerializeField] private MusicTrack raceMusic;

    private float clockTime = float.MaxValue;
    public BooleanProperty clockLayerActivated;
    [Range(0, 1)] public float percentageToClock;

    public float timeSilent;
    public float timeFading;

    private float targetGhostTime;
    private float timeRaceStarted = float.MaxValue;
    private float raceFadeOutDelay;


    private void Start()
    {
        IRaceController.OnRacePrepStart += OnRaceEnter;
        IRaceController.OnRaceStart += OnRaceStart;
        IRaceController.OnCompleteRaceObjective += OnRaceObjectiveCompleted;
    }
    private void OnDestroy()
    {
        StopAllCoroutines();
        IRaceController.OnRacePrepStart -= OnRaceEnter;
        IRaceController.OnRaceStart -= OnRaceStart;
        IRaceController.OnCompleteRaceObjective -= OnRaceObjectiveCompleted;
    }

    private void Update() 
    {
        if(clockLayerActivated) clockLayerActivated.Value = Time.time >= clockTime && Time.time <= timeRaceStarted + raceFadeOutDelay;
    }

    private void OnRaceEnter()
    {
        StopAllCoroutines();
        if (raceMusic != null)
        {
            StartCoroutine(StartMusic(raceMusic, 0.1f, 2.8f));
        }

        targetGhostTime = (float)IRaceController.CurrentRace.TargetGhostTime;
    }
    private void OnRaceStart()
    {
        StartCoroutine(Counter());

        timeRaceStarted = Time.time;
        clockTime = timeRaceStarted + targetGhostTime * percentageToClock;
        raceFadeOutDelay = targetGhostTime - timeFading - timeSilent;

        Debug.Log("target ghost time " + targetGhostTime);


        StartCoroutine(SetFade(0, timeFading, raceFadeOutDelay));
    }

    private void OnRaceObjectiveCompleted()
    {
        StopAllCoroutines();
        StartCoroutine(StopMusic(1));
    }

    private IEnumerator StartMusic(MusicTrack track, float fadeInTime, float delay = 0)
    {
        yield return new WaitForSeconds(delay);

        if(musicManager) musicManager.StartMusic(track, fadeInTime);
    }
    private IEnumerator SetFade(float targetVolume,  float fadeTime, float delay = 0)
    {
        yield return new WaitForSeconds(delay);

        if (musicManager) raceMusic.SetFade(targetVolume, fadeTime);
    }

    private IEnumerator StopMusic(float fadeTime, float delay = 0)
    {
        yield return new WaitForSeconds(delay);


        if (musicManager) musicManager.StopMusic(fadeTime);
    }

    private IEnumerator Counter()
    {
        yield return new WaitForSeconds(targetGhostTime);
        Debug.Log("Ghost won");
    }
}
