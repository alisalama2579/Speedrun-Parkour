using System.Collections.Generic;
using System;
using UnityEngine;

public interface IRaceController
{
    public static IRaceController CurrentRace = null;
    public const int COUNTDOWNS = 3;
    public const float ABSOLUTE_MAX_PREP_TIME = 10f;

    public static Action OnRacePrepStart;
    public static Action OnRaceStart;
    public static Action OnRaceExit;
    public static Action OnCompleteRaceObjective;
    public static Action<int> OnCountDown;
    public Vector2 StartingPos { get; }
    public int FacingDir { get; }
    public IState CurrentState { get; }
    public double TargetGhostTime { get; }
    public PlayerRaceStats RaceStats { get; }
}

public struct PlayerRaceStats
{
    public bool Passed;
    public bool Perfected;
    public double RecordTime;
    public int Attempts;
}
public enum TargetGhostSelectionType
{
    Least,
    Greatest,
    ListOrder
}
