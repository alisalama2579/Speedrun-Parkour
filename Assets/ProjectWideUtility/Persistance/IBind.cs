
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace System.Persistence
{
    public interface IBind<TData> where TData : ISaveable
    {
        SerializableGuid ID { get; set; }
        void Bind(TData data);
    }
}
