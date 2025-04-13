
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace System.Persistence
{
    public interface ISerializer
    {
        void Serialize<T>(string path, T data);
        T Deserialize<T>(string name);
    }
}
