using System;
using System.Collections.Generic;

namespace MFSJSoft.Data
{
    public class DictionaryProperties<V> : IProperties
    {
        readonly IDictionary<string, V> dictionary;

        public DictionaryProperties(IDictionary<string, V> dictionary)
        {
            this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        public string GetProperty(string name)
        {
            return dictionary.TryGetValue(name, out var value) ? value.ToString() : null;
        }
    }
}
