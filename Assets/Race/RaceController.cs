using System;
using UnityEngine;

public class RaceController : MonoBehaviour
{
    public static event Action OnRaceStart;
    public static event Action OnRaceEnd;

    public RaceStart raceStart;
    public RaceEnd raceEnd;


    private void Awake()
    {
        raceStart.onPlayerEnter += RaceStarted;
        raceEnd.onPlayerEnter += RaceEnded;
    }

    private void RaceStarted()
    {
        OnRaceStart?.Invoke();
    }

    private void RaceEnded()
    {
        OnRaceEnd?.Invoke();
    }

}
