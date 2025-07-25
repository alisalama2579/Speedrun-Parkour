using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GhostTapePlayer))]
public class RaceGhost : MonoBehaviour
{
    public int startDelay;
    private GhostTapePlayer tapePlayer;
    public RaceGhostInfo info;

    private void Start()
    {
        tapePlayer = GetComponent<GhostTapePlayer>();
        IRaceController.OnRaceStart += OnRaceStart;
        IRaceController.OnRacePrepStart += OnRaceEnter;
    }

    public void OnRaceEnter()
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
        IRaceController.OnRacePrepStart -= OnRaceEnter;
    }

}
