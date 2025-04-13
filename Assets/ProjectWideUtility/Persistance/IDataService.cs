using System.Collections.Generic;
using System.Persistence;

public interface IDataService<T> where T : ISaveData
{
    void Save(T data, bool overWrite = true);
    T Load(string name);
    void Delete(string name);
    void DeleteAll();
}
