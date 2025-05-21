using UnityEngine;

public interface IDamaging
{
    public enum DamageType
    {
        Normal,
        Instakill,
    }
    public struct SourceInfo
    {
        public int Damage;
        public float Knockback;
        public DamageType Type;
    }

    public SourceInfo Info { get; }
}

