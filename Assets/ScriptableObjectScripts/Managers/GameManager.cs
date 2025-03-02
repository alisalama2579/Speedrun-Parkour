using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private void Awake()
    {
        if (Instance) Instance = this;
        else Destroy(this);
    }

    public enum GameState
    {
        StartScreen,
        Paused,
        Play,
        Race
    }
    private const GameState STARTING_GAME_STATE = GameState.Race;
    public GameState gameState = STARTING_GAME_STATE;

    public event Action GameManagerAwake;
    public event Action GameManagerUpdate;

}
