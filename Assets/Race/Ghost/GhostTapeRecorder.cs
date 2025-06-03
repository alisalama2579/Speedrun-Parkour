using System.Collections.Generic;
using UnityEngine;

public class GhostTapeRecorder : MonoBehaviour
{
    [SerializeField] private Animator playerAnimator;
    [SerializeField] private int recordIndex;

    private Transform targetTransform;
    private Vector2 startingPos;

    private List<CompressedGhostFrameValues> frameRecord;

    private void Start()
    {
        targetTransform = transform;
        frameRecord = new();

        startingPos = targetTransform.position;
    }

    private int frame;
    private void FixedUpdate()
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

    private void OnDestroy()
    {
        if (enabled) 
            SaveTape();
    }

    public void SaveTape() => RecordsManager.SetRecord(frameRecord, recordIndex);

    [ContextMenu("test saving")]
    public void Test() => RecordsManager.Test();
}
