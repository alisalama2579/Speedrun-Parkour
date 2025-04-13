using System.Collections.Generic;
using UnityEngine;

public class GhostTapeRecorder : MonoBehaviour
{
    [SerializeField] private PlayerAnimator animator;
    [SerializeField] private RecordsHolder recordsHolder;
    [SerializeField] private int recordIndex;

    private Transform targetTransform;

    private List<RecordsTest.RecordFrameValues> frameRecord;

    private void Start()
    {
        targetTransform = transform;
        frameRecord = new();

        RecordsTest.Test();
    }

    private void FixedUpdate()
    {
        RecordsTest.RecordFrameValues frameValue = new RecordsTest.RecordFrameValues 
        { 
            position = targetTransform.position,
        };
        frameRecord.Add(frameValue);
    }

    private void OnDestroy()
    {
        if (enabled) { SaveTape(); }
    }

    public void SaveTape()
    {
        RecordsTest.SetRecord(frameRecord, recordIndex);
    }
}
