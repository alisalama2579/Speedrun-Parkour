using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "LevelMechanicStats", menuName = "LevelMechanicStats")]
public class LevelMechanicStats : ScriptableObject
{
    [Space(5)]

    #region Sand

    [Header("Sand")]
    [Range(0, 100)] public float fadeTime;
    [Range(0, 100)] public float fadeDelay;

    public float sandLaunchSpeed;
    public float burrowLaunchSpeed;
    public float burrowWeakLaunchSpeed;
    public float sandColliderReactivationDelay;

    #endregion


    [Space(5)]

    #region Secrets

    [Header("Secrets")]
    [Range(0, 100)] public float secretFadeTime;

    #endregion


    [Space(5)]

    #region SlipperyGround

    [Header("SlipperyGround")]
    [Range(0, 100)] public float slipperiness;

    #endregion

}
