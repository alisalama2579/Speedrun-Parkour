using System.Collections.Generic;
using UnityEngine;

public class GhostTapePlayer : MonoBehaviour
{
    [SerializeField] private int recordIndex;

    private List<CompressedGhostFrameValues> frameRecord;
    private GhostFrameValues targetValues;
    private GhostFrameValues currentValues;
    private Vector2 startingPos;

    private void Start()
    {
        frameRecord = RecordsManager.GetRecord(recordIndex);
        startingPos = transform.position;
    }   

    private float frame;
    private void FixedUpdate()
    {
        float progress = frame / RecordsManager.framesPerValue;

        if (progress < frameRecord.Count)
        {
            float modulus = frame % RecordsManager.framesPerValue;
            if (modulus == 0)
            {
                int index = (int)progress;
                currentValues = GhostFrameConversions.ToUncompressed(frameRecord[index]);
                targetValues = GhostFrameConversions.ToUncompressed(
                    index + 1 == frameRecord.Count 
                    ?  frameRecord[index] 
                    : frameRecord[index + 1]);
            }

            transform.position = startingPos + Vector2.Lerp(currentValues.pos, targetValues.pos, modulus/RecordsManager.framesPerValue);
            transform.eulerAngles = Vector3.forward * Mathf.Lerp(currentValues.zRot, targetValues.zRot, progress);

            Debug.Log("Modulus: " + modulus);
        }
        Debug.Log("Progress: " + progress);
        frame++;
    }
}
