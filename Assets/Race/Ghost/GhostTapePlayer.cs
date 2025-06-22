using System;
using System.Collections.Generic;
using UnityEngine;

public class GhostTapePlayer : MonoBehaviour
{
    [SerializeField] private int recordIndex;
    [SerializeField] private Animator animator;

    [SerializeField] public float recordTime;
    public bool playing;

    private List<CompressedGhostFrameValues> frameRecord;
    private GhostFrameValues targetValues;
    private GhostFrameValues currentValues;
    private Vector2 startingPos;

    public void Awake()
    {
        startingPos = transform.position;
        frameRecord = RecordsManager.GetRecord(recordIndex).ghostFrameValues;
        frame = 0;
    }
    public void StartTape()
    {
        playing = true;
    }

    public void ResetTape()
    {
        StopTape();
        SetFrameValues(GhostFrameConversions.ToUncompressed(frameRecord[0]));
    }
    public void StopTape()
    {
        playing = false;
        frame = 0;
    }
    public void PauseTape() => playing = false;

    private float frame;
    private int animID;
    public void FixedUpdate()
    {
        if (!playing) return;

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

                int id = targetValues.animID;
                if (animID != id)
                {
                    animator.CrossFade(id, 0);
                    animator.speed = currentValues.animSpeed;
                }
                animID = id;
            }

            transform.position = startingPos + Vector2.Lerp(currentValues.pos, targetValues.pos, modulus/RecordsManager.framesPerValue);
            transform.eulerAngles = Vector3.forward * Mathf.Lerp(currentValues.zRot, targetValues.zRot, progress);
        }
        frame++;
    }

    private void SetFrameValues(GhostFrameValues values)
    {
        transform.position = startingPos + values.pos;
        transform.eulerAngles = Vector3.forward * values.zRot;
        animator.CrossFade(values.animID, 0);
        animator.speed = values.animSpeed;
    }
}
