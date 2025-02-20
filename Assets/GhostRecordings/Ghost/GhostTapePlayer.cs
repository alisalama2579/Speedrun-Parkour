using System.Collections.Generic;
using UnityEngine;

public class GhostTapePlayer : MonoBehaviour
{
    [SerializeField] private PlayerAnimator animator;

    [SerializeField] private RecordsHolder recordsHolder;
    [SerializeField] private int recordIndex;
    private Transform targetTransform;


    private List<RecordsHolder.RecordFrameValues> frameRecord;

    private void Start()
    {
        targetTransform = transform;
        frameRecord = recordsHolder.GetRecord(recordIndex);
        animator.InitializeAnimator();
    }

    private int frame;
    private void FixedUpdate()
    {
        if (frame < frameRecord.Count)
        {
            RecordsHolder.RecordFrameValues frameValues = frameRecord[frame];

            targetTransform.position = frameValues.position;
            animator.UpdateAnimator(new PlayerAnimator.AnimationValues
            {
                isGrounded = frameValues.isGrounded,
                isOnWall = frameValues.isOnWall,
                velocity = frameValues.velocity,
                moveInput = frameValues.moveInput,
            });
        }
        frame++;
    }
}
