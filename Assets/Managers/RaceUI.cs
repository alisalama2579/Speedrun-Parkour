using UnityEngine;

public class RaceUI : MonoBehaviour
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
    private void ActivateRaceStartUI()
    {
        if (RaceStartUI == null) return;
        RaceStartUI.Display();
    }

    private void ActivateRaceEndUI()
    {
        if (RaceEndUI == null) return;
        RaceEndUI.Display();
    }
}
