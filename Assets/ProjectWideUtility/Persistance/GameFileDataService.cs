using System.Collections.Generic;
using System.IO;
using System.Persistence;
using UnityEngine;

public class GameFileDataService : IDataService<GameData>
{
    readonly ISerializer serializer;
    readonly string dataPath;
    readonly string fileExtension;

    public GameFileDataService(ISerializer serializer)
    {
        this.serializer = serializer;
        dataPath = Application.persistentDataPath;
        fileExtension = "save";
    }

    string GetPathToFile(string fileName) => Path.Combine(dataPath, $"{fileName}.{fileExtension}");

    public void Save(GameData data, bool overWrite = true)
    {
        string fileName = data.Name;
        string fileLocation = GetPathToFile(fileName);

        if (!overWrite && File.Exists(fileLocation))
            throw new IOException($"The file '{fileName}.{fileExtension}' already exists and cannot be overwritten");

        serializer.Serialize(fileLocation, data);
    }

    public GameData Load(string name)
    {
        string fileLocation = GetPathToFile(name);

        if (!File.Exists(fileLocation))
            throw new IOException($"No persisted game data with name '{name}'");

        return serializer.Deserialize<GameData>(fileLocation);
    }

    public void Delete(string name)
    {
        string fileLocation = GetPathToFile(name);

        if (File.Exists(fileLocation))
            File.Delete(fileLocation);
    }

    public void DeleteAll()
    {
        foreach (string filePath in Directory.GetFiles(dataPath))
        {
            if (Path.GetExtension(filePath) == fileExtension)
                File.Delete(filePath);
        }
    }
}
