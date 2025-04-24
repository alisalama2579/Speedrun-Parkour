using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GhostTapePlayer))]
public class Ghost : MonoBehaviour
{
    public int startDelay;
    private GhostTapePlayer tapePlayer;

    private void Awake()
    {
        tapePlayer = GetComponent<GhostTapePlayer>();
    }

    private void FixedUpdate()
    {
        if(Time.timeSinceLevelLoad > startDelay)
            tapePlayer.UpdateTape();
    }
}
