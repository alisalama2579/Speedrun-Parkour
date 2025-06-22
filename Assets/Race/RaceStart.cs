using System;
using UnityEngine;

public class RaceStart : MonoBehaviour, IPlayerCollisionInteractor
{
    public event Action onPlayerEnter;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out PlayerController _)) onPlayerEnter?.Invoke();
    }
}
