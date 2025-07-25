using System.Collections.Generic;
using UnityEngine;

public class GhostTapeRecorder : MonoBehaviour
{
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private bool recordOnRaceStart;
    [SerializeField] private int recordIndex;

    private Transform targetTransform;
    private Vector2 startingPos;

    private List<CompressedGhostFrameValues> frameRecord;

    public RaceGhostInfo raceGhostInfo;
    private double timeRaceStarted;

    private bool active;

    private void Start()
    {
        targetTransform = transform;
        frameRecord = new();

        if (!recordOnRaceStart)
        {
            active = true;
            startingPos = targetTransform.position;
        }

        IRaceController.OnRaceStart += () =>
        {
            if (recordOnRaceStart)
            {
                startingPos = targetTransform.position;
                active = true;
                timeRaceStarted = Time.timeAsDouble;
            }
        };
        IRaceController.OnCompleteRaceObjective += () =>
        {
            if (raceGhostInfo != null && recordOnRaceStart) raceGhostInfo.raceTime = Time.timeAsDouble - timeRaceStarted;
        };
    }

    private int frame;
    private void FixedUpdate()
    {
        if (active)
        {
            if (frame % RecordsManager.framesPerValue == 0)
            {
                CompressedGhostFrameValues frameValue = GhostFrameConversions.ToCompressed(
                    new GhostFrameValues()
                    {
                        pos = (Vector2)targetTransform.position - startingPos,
                        zRot = playerAnimator.transform.eulerAngles.z,
                        animID = playerAnimator.GetCurrentAnimatorStateInfo(0).fullPathHash,
                        animSpeed = (uint)playerAnimator.GetCurrentAnimatorStateInfo(0).speedMultiplier
                    });

                frameRecord.Add(frameValue);
            }
            frame++;
        }
    }

    private void OnDestroy()
    {
        if (enabled) 
            SaveTape();
    }

    public void SaveTape() => RecordsManager.SetRecord(new GhostTape(frameRecord), recordIndex);

    [ContextMenu("test saving")]
    public void Test() => RecordsManager.Test();
}
