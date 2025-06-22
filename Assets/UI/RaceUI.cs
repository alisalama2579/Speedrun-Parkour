using UnityEngine;

public class RaceUI : MonoBehaviour
{
    [SerializeField] private UIBase RaceStartUI;
    [SerializeField] private UIBase RaceEndUI;

    //private void Awake()
    //{
    //    RaceController.OnRaceEnter += ActivateRaceStartUI;
    //    RaceController.OnRaceEnter += ActivateRaceEndUI;
    //}
    //private void OnDisable()
    //{
    //    RaceController.OnRaceEnter -= ActivateRaceStartUI;
    //    RaceController.OnRaceEnter -= ActivateRaceEndUI;
    //}
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
