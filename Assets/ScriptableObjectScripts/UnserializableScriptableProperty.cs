using System;
using UnityEngine;
public class UnserializableScriptableProperty<T> : ScriptableObject
{
    [SerializeField] protected bool UseConstant = true;
    protected T value;

    public T Value
    {
        get { return value; }
        set { if (!UseConstant) this.value = value; }
    }
}
