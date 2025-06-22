using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GhostTapePlayer))]
public class RaceGhost : MonoBehaviour
{
    public int startDelay;
    public float raceTime;
    private GhostTapePlayer tapePlayer;

    private void Awake()
    {
        tapePlayer = GetComponent<GhostTapePlayer>();
        raceTime = tapePlayer.recordTime + startDelay;
        IRaceController.OnRaceStart += OnRaceStart;
        IRaceController.OnRaceEnter += OnRaceEnter;
    }
    public void OnRaceEnter(IRaceController _)
    {
        tapePlayer.ResetTape();
    }
    public void OnRaceStart()
    {
        StartCoroutine(StartTape());
    }
    private IEnumerator StartTape()
    {
        yield return new WaitForSeconds(startDelay);
        tapePlayer.StartTape(); 
    }

    private void OnDestroy()
    {
        IRaceController.OnRaceStart -= OnRaceStart;
        IRaceController.OnRaceEnter -= OnRaceEnter;
    }
}
