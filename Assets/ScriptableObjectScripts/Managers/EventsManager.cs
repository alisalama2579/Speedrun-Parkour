using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EventsManager", menuName = "EventsManager")]
public class EventsManager : ScriptableObject
{
    public void OnAwake()
    {
        Instance = this;
    }

    public static EventsManager Instance;

    public event Action OnPlayerLandOnStableGround;
    public void InvokePlayerLandOnStableGround() => OnPlayerLandOnStableGround?.Invoke();

    public event Action<Player.HurtValues> OnPlayerHurt;
    public void InvokePlayerHurt(Player.HurtValues hurtValues) => OnPlayerHurt?.Invoke(hurtValues);

    public event Action OnPlayerDeath;
    public void InvokePlayerDeath() => OnPlayerDeath?.Invoke();

    public event Action OnRaceStart;
    public void InvokeRaceStart() => OnRaceStart?.Invoke();

    public event Action OnRaceEnd;
    public void InvokeRaceEnd() => OnRaceEnd?.Invoke();

}
