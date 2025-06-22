using UnityEngine;
using System.Collections.Generic;
using System;
using System.Persistence;

public static class RecordsManager
{
    static GhostTape[] levelRecords = new GhostTape[LIST_COUNT];
    const int LIST_COUNT = 10;
    public static int framesPerValue = 4;

    public static void SetRecord(GhostTape frameRecord, int index)
    {
        if (index >= levelRecords.Length) return;

        Debug.Log($"Set record number {index}");

        levelRecords[index] = frameRecord;
        RecordSaveLoadSystem.Save(levelRecords);
    }

    public static GhostTape GetRecord(int index)
    {
        if (index >= levelRecords.Length) return new GhostTape(null);

        RecordSaveLoadSystem.Load();
        Debug.Log($"Got record number {index}");

        levelRecords = RecordSaveLoadSystem.recordData.Records;
        return levelRecords[index];
    }

    public static void Test()
    {
        if (levelRecords == null) Debug.Log("Records did not save");
        else Debug.Log("Records saved");
    }
}

