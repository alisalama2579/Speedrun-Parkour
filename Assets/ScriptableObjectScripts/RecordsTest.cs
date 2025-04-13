using UnityEngine;
using System.Collections.Generic;
using System;

public static class RecordsTest
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

    [SerializeField] static LevelRecord[] levelRecords = new LevelRecord[3];
    public static float listCount;

    public static void SetRecord(List<RecordFrameValues> frameRecords, int index)
    {
        if (index >= levelRecords.Length) return;

        listCount++;

        levelRecords[index].recordFrameValues = new();
        for (int i = 0; i < frameRecords.Count; i++)
        {
            levelRecords[index].recordFrameValues.Add(frameRecords[i]);
        }
    }

    public static List<RecordFrameValues> GetRecord(int index)
    {
        if (index >= levelRecords.Length) return new List<RecordFrameValues>();
        return levelRecords[index].recordFrameValues;
    }

    public static void Test()
    {
        if (levelRecords == null) Debug.Log("Records did not save");
        else Debug.Log("Records saved");
    }
}
