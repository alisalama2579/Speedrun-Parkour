using System.IO;
using System.Persistence;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization.Formatters.Binary;

public class BinarySerializer : ISerializer
{
    public void Serialize<T>(string path, T obj)
    {
        using FileStream stream = File.Open(path, FileMode.Create);
        BinaryFormatter formatter = new();
        formatter.Serialize(stream, obj);
    }

    public T Deserialize<T>(string path)
    {
        using FileStream stream = File.Open(path, FileMode.Open);
        if (stream == null) return default;

        BinaryFormatter formatter = new();
        return (T)formatter.Deserialize(stream);
    }
}
