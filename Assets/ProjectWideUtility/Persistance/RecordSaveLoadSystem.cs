using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

namespace System.Persistence
{
    public static class RecordSaveLoadSystem
    {
        const string NEW_RECORD_NAME = "New Record";

        public static RecordData recordData
        {
            get
            {
                if (recordData != null) return recordData;
                return recordData = new RecordData(NEW_RECORD_NAME);
            }
            set => recordData = value;
        }


        static IDataService<RecordData> dataService
        {
            get
            {
                if (dataService != null) return dataService;
                return new RecordFileDataService(new BinarySerializer());
            }
            set => dataService = value;
        }



        public static void NewRecord()
        {
            recordData = new RecordData(NEW_RECORD_NAME);
        }

        public static void SaveGame() => dataService.Save(recordData);
        public static void DeleteGame(string gameName) => dataService.Delete(gameName);
        public static void ReloadGame() => dataService.Load(recordData.Name);
        public static void LoadGame(string gameName)
        {
            recordData = dataService.Load(gameName);
        }
    }


    [Serializable]
    public class RecordData : ISaveData
    {
        public string Name { get; set; }

        public RecordData(string name) => Name = name;
    }
}
