using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializableDictionary<TKey, TValue>
{
    [SerializeField] private Pair[] pairs;

    public Dictionary<TKey, TValue> GenerateDictionary()
    {
        Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        for (int i = 0; i < pairs.Length; i++)
            dictionary.Add(pairs[i].key, pairs[i].value);

        //GC
        pairs = null;
        return dictionary;

    }

    [Serializable]
    public class Pair
    {
        public TKey key;
        public TValue value;
    }
}
