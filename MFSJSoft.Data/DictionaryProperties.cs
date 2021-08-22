using System;
using System.Collections.Generic;

namespace MFSJSoft.Data
{

    /// <summary>
    /// A simple implementation of <see cref="IProperties"/> that uses a backing <see cref="IDictionary{TKey, TValue}"/>.
    /// </summary>
    /// <typeparam name="V">Generally <see cref="string"/>, however if another type, it will be
    /// <see cref="object.ToString">converted</see> to a <see cref="string"/> and returned.</typeparam>
    public class DictionaryProperties<V> : IProperties
    {
        readonly IDictionary<string, V> dictionary;

        /// <summary>
        /// Constructs a <see cref="DictionaryProperties{V}"/> with the given backing <see cref="IDictionary{TKey, TValue}"/>
        /// </summary>
        /// <param name="dictionary">Backing <see cref="IDictionary{TKey, TValue}"/>.</param>
        public DictionaryProperties(IDictionary<string, V> dictionary)
        {
            this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
        }

        /// <summary>
        /// Returns the value in the backing <see cref="IDictionary{TKey, TValue}"/> with the given <c>name</c>.
        /// If no key exists in the dictionary equal to <c>name</c>, <see langword="null"/> is returned. If
        /// the value in the dictonary is not a string, the result of its <see cref="object.ToString"/> is returned.
        /// </summary>
        /// <param name="name">Key to look up in the backing <see cref="IDictionary{TKey, TValue}"/></param>
        /// <returns>Value whose key is the given <c>name</c> if it exists, else <see langword="null" />. Values
        /// that are not strings are converted using their <see cref="object.ToString"/> method.</returns>
        public string GetProperty(string name)
        {
            return dictionary.TryGetValue(name, out var value) ? value.ToString() : null;
        }
    }
}
