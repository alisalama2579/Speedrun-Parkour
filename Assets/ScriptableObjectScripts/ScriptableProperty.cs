using System;
using UnityEngine;

public class ScriptableProperty<T> : ScriptableObject
{
    [SerializeField] protected bool UseConstant = true;
    [SerializeField] protected T value;

    public T Value
    {
        get { return value; }
        set { if (!UseConstant) this.value = value;  }
    }
}

