using System.Collections.Generic;
using UnityEngine;

public class GhostTapePlayer : MonoBehaviour
{
    [SerializeField] private int recordIndex;
    private Transform targetTransform;


    private List<RecordsTest.RecordFrameValues> frameRecord;

    private void Start()
    {
        targetTransform = transform;
        frameRecord = RecordsTest.GetRecord(recordIndex);
    }

    private int frame;
    private void FixedUpdate()
    {
        if (frame < frameRecord.Count)
        {
            RecordsTest.RecordFrameValues frameValues = frameRecord[frame];

            targetTransform.position = frameValues.position;
        }
        frame++;
    }
}
