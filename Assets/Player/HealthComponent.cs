using System;
using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    [SerializeField] private IHaveHealth entity;

    [SerializeField] private float currentInvincibilityPeriod = 2f;
    [SerializeField] private int maxHealth;
    public int health;

    public event Action<IDamaging.SourceInfo> OnDamage;
    public event Action<IDamaging.SourceInfo> OnDeath;

    private void Awake()
    {
        entity.OnHealthChange += TakeDamage;
    }

    private float timeTookDamage = float.MinValue;
    private void TakeDamage(IDamaging.SourceInfo info)
    {
        if (info.Type == IDamaging.DamageType.Instakill)
        {
            health = 0;
            OnDeath?.Invoke(info);

            return;
        }

        else if(Time.time > timeTookDamage + currentInvincibilityPeriod)
        {
            health -= info.Damage;

            if (health <= 0)
            {
                health = 0;
                OnDeath?.Invoke(info);

                return;
            }

            timeTookDamage = Time.time;
            OnDamage?.Invoke(info);
        }   
    }

    private void ChangeHealth()
    {

    }

    private void OnDestroy()
    {
        entity.OnHealthChange -= TakeDamage;
    }
}

public interface IHaveHealth
{
    public event Action<IDamaging.SourceInfo> OnHealthChange;
}
