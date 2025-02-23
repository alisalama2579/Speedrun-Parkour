using System.Collections.Generic;
using UnityEngine;

public class GhostTapeRecorder : MonoBehaviour
{
    [SerializeField] private PlayerAnimator animator;
    [SerializeField] private RecordsHolder recordsHolder;
    [SerializeField] private int recordIndex;

    private Transform targetTransform;

    private List<RecordsHolder.RecordFrameValues> frameRecord;

    private void Start()
    {
        targetTransform = transform;
        frameRecord = new();
    }

    private void FixedUpdate()
    {
        RecordsHolder.RecordFrameValues frameValue = new RecordsHolder.RecordFrameValues 
        { 
            position = targetTransform.position,
            isGrounded = animator.animationFrameValues.isGrounded,
            isOnWall = animator.animationFrameValues.isOnWall,
            moveInput = animator.animationFrameValues.moveInput,
            velocity = animator.animationFrameValues.velocity

        };
        frameRecord.Add(frameValue);
    }

    private void OnDestroy()
    {
        if (enabled) { SaveTape(); }
    }

    public void SaveTape()
    {
        recordsHolder.SetRecord(frameRecord, recordIndex);
    }
}
