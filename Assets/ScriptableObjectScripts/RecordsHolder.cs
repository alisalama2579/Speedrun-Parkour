using UnityEngine;
using System.Collections.Generic;
using System;

[CreateAssetMenu(fileName = "RecordsHolder", menuName = "RecordsHolder")]
public class RecordsHolder : ScriptableObject
{
    [Serializable]
    private class LevelRecord
    {
        public List<RecordFrameValues> recordFrameValues;
    }

    public struct RecordFrameValues
    {
        public Vector3 position;
        public bool isOnWall;
        public bool isGrounded;
        public float moveInput;
        public Vector2 velocity;
    }

    [SerializeField] LevelRecord[] levelRecords = new LevelRecord[3];
    public float listCount;

    public void SetRecord(List<RecordFrameValues> frameRecords, int index)
    {
        if (index >= levelRecords.Length) return;

        listCount++;

        levelRecords[index].recordFrameValues = new();
        for (int i = 0; i < frameRecords.Count; i++)
        {
            levelRecords[index].recordFrameValues.Add(frameRecords[i]);
        }
    }

    public List<RecordFrameValues> GetRecord(int index)
    {
        if (index >= levelRecords.Length) return new List<RecordFrameValues>();
        return levelRecords[index].recordFrameValues;
    }
}
