using System.Collections.Generic;
using System;
using UnityEngine;

public interface IRaceController
{
    public static IRaceController currentRace;
    public const float ABSOLUTE_MAX_PREP_TIME = 10f;

    public static Action<IRaceController> OnRaceEnter;
    public static Action OnRaceStart;
    public static Action OnRaceExit;
    public static Action OnCompleteRaceObjective;
    public static Action<int> OnCountDown;
    public Vector2 StartingPos { get; }
    public int CountDowns { get; }
    public RaceState CurrentState { get; }
    public PlayerRaceStats RaceStats { get; }

    public void RaceEnter()
    {

    }
    public void RaceStart()
    {

    }
    public void RaceEnd()
    {

    }
    public void RaceExit()
    {

    }
}

public struct PlayerRaceStats
{
    public bool Passed;
    public bool Perfected;
    public double RecordTime;
    public int Attempts;
}

public enum RaceState
{
    NotInRace,
    Prep,
    InRace,
    CompletedObjective
}