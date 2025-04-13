using System.Collections.Generic;
using System.IO;
using System.Persistence;
using UnityEngine;

public class RecordFileDataService : IDataService<RecordData>
{
    readonly ISerializer serializer;
    readonly string dataPath;
    readonly string fileExtension;

    public RecordFileDataService(ISerializer serializer)
    {
        this.serializer = serializer;
        dataPath = Application.persistentDataPath;
        fileExtension = "save";
    }

    string GetPathToFile(string fileName) => Path.Combine(dataPath, $"{fileName}.{fileExtension}");

    public void Save(RecordData data, bool overWrite = true)
    {
        #if UNITY_EDITOR

        string fileName = data.Name;
        string fileLocation = GetPathToFile(fileName);

        if (!overWrite && File.Exists(fileLocation))
            throw new IOException($"The file '{fileName}.{fileExtension}' already exists and cannot be overwritten");

        serializer.Serialize(fileLocation, data);

        #endif
    }

    public RecordData Load(string name)
    {
        string fileLocation = GetPathToFile(name);

        if (!File.Exists(fileLocation))
            throw new IOException($"No persisted game data with name '{name}'");

        return serializer.Deserialize<RecordData>(fileLocation);
    }

    public void Delete(string name)
    {
        #if UNITY_EDITOR

        string fileLocation = GetPathToFile(name);

        if (File.Exists(fileLocation))
            File.Delete(fileLocation);
        
        #endif
    }

    public void DeleteAll()
    {
        #if UNITY_EDITOR

        foreach (string filePath in Directory.GetFiles(dataPath))
        {
            if (Path.GetExtension(filePath) == fileExtension)
                File.Delete(filePath);
        }

        #endif
    }
}
