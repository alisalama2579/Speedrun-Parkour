using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private UIBase RaceStartUI;
    [SerializeField] private UIBase RaceEndUI;

    private void Awake()
    {
        RaceController.OnRaceStart += ActivateRaceStartUI;
        RaceController.OnRaceStart += ActivateRaceEndUI;
    }
    private void OnDisable()
    {
        RaceController.OnRaceStart -= ActivateRaceStartUI;
        RaceController.OnRaceStart -= ActivateRaceEndUI;
    }
    private void ActivateRaceStartUI() => RaceStartUI?.StartUI();
    private void ActivateRaceEndUI() => RaceEndUI?.StartUI();
}
