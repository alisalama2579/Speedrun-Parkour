namespace System.Persistence
{
    public static class RecordSaveLoadSystem
    {
        const string NEW_DATA_NAME = "New Data";

        public static RecordData recordData = new RecordData(NEW_DATA_NAME, null);

        static IDataService<RecordData> dataService = new RecordFileDataService(new BinarySerializer());

        public static void NewRecord(LevelRecord[] records)
        {
            recordData = new RecordData(NEW_DATA_NAME, records);
        }

        public static void Save(LevelRecord[] records) 
        {
            recordData = new RecordData(NEW_DATA_NAME, records);
            dataService.Save(recordData);
        }
        public static void Delete() => dataService.Delete(recordData.Name);
        public static void Load() => recordData = dataService.Load(recordData.Name);
    }


    [Serializable]
    public class RecordData : ISaveData
    {
        public string Name { get; set; }
        public LevelRecord[] Records { get; set; }
        public RecordData(string name, LevelRecord[] records) 
        {
            Records = records;
            Name = name;
        } 
    }
}
