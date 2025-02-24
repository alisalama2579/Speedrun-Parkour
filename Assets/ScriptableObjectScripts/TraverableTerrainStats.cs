using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "TraversableTerrainStats", menuName = "TraversableTerrainStats")]
public class TraversableTerrainStats : ScriptableObject
{
    [Space(5)]

    #region Sand

    [Header("Sand")]
    [Range(0, 100)] public float fadeTime;
    [Range(0, 100)] public float fadeDelay;

    #endregion


    [Space(5)]

    #region SlipperyGround

    [Header("SlipperyGround")]
    [Range(0, 100)] public float slipperiness;

    #endregion

}
