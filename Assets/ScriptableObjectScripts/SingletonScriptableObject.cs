using UnityEngine;

public abstract class SingletonScriptableObject : ScriptableObject
{
    public static SingletonScriptableObject Instance;

    private void OnValidate() => Init();
    private void Awake() => Init();

    private void Init()
    {
        if (Instance == null) Instance = this;
        else DestroyImmediate(this);
    }
}
