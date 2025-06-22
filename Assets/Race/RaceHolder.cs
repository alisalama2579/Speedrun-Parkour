using System.Collections.Generic;
using UnityEngine;

public class RaceHolder : ScriptableObject
{
    public MiniRace[] races;
    [HideInInspector] public Dictionary<MiniRace, float> playerBestTimes;

}
