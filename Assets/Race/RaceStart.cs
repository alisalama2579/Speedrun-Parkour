using System;
using UnityEngine;

public class RaceStart : MonoBehaviour
{
    public event Action onPlayerEnter;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Player _)) onPlayerEnter?.Invoke();
    }
}
