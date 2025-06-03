using UnityEngine;
using System.Collections.Generic;
using System;
using System.Persistence;

public static class RecordsManager
{
    static LevelRecord[] levelRecords = new LevelRecord[LIST_COUNT];
    const int LIST_COUNT = 10;
    public static int framesPerValue = 4;

    public static void SetRecord(List<CompressedGhostFrameValues> frameRecord, int index)
    {
        if (index >= levelRecords.Length) return;

        Debug.Log($"Set record number {index}");

        levelRecords[index] = new LevelRecord(frameRecord);
        RecordSaveLoadSystem.Save(levelRecords);
    }

    public static List<CompressedGhostFrameValues> GetRecord(int index)
    {
        if (index >= levelRecords.Length) return new List<CompressedGhostFrameValues>();

        RecordSaveLoadSystem.Load();
        Debug.Log($"Got record number {index}");

        levelRecords = RecordSaveLoadSystem.recordData.Records;
        return levelRecords[index].ghostFrameValues;
    }

    public static void Test()
    {
        if (levelRecords == null) Debug.Log("Records did not save");
        else Debug.Log("Records saved");
    }
}


[Serializable]
public class LevelRecord
{
    public List<CompressedGhostFrameValues> ghostFrameValues;
    public LevelRecord(List<CompressedGhostFrameValues> ghostFrameValues)
    {
        this.ghostFrameValues = ghostFrameValues;
    }
}
