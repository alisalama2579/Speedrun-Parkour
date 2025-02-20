using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

[CreateAssetMenu(fileName = "RecordsHolder", menuName = "RecordsHolder")]
public class RecordsHolder : ScriptableObject
{
    public struct RecordFrameValues
    {
        public Vector3 position;
        public bool isOnWall;
        public bool isGrounded;
        public float moveInput;
        public Vector2 velocity;
    }

    public List<RecordFrameValues>[] levelRecords = new List<RecordFrameValues>[3];

    public void SetRecord(List<RecordFrameValues> frameRecords, int index)
    {
        if (index >= levelRecords.Length) return;

        levelRecords[index] = new();
        for (int i = 0; i < frameRecords.Count; i++)
        {
            levelRecords[index].Add(frameRecords[i]);
        }
    }

    public List<RecordFrameValues> GetRecord(int index)
    {
        if (index >= levelRecords.Length) return new List<RecordFrameValues>();
        return levelRecords[index];
    }
}
