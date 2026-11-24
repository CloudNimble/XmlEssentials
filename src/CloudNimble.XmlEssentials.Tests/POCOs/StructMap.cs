using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A dictionary that is a value type. It can be created and replaced, and cannot be populated in place.
    /// </summary>
    public struct StructMap : IDictionary<string, int>
    {

        #region Fields

        private readonly Dictionary<string, int> _entries;

        #endregion

        #region Public Properties

        /// <inheritdoc />
        public readonly int Count => _entries.Count;

        /// <inheritdoc />
        public readonly bool IsReadOnly => false;

        /// <inheritdoc />
        public readonly ICollection<string> Keys => _entries.Keys;

        /// <inheritdoc />
        public readonly ICollection<int> Values => _entries.Values;

        /// <inheritdoc />
        public readonly int this[string key]
        {
            get => _entries[key];
            set => _entries[key] = value;
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new, empty instance of the <see cref="StructMap"/> struct.
        /// </summary>
        public StructMap()
        {
            _entries = [];
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public readonly void Add(string key, int value) => _entries.Add(key, value);

        /// <inheritdoc />
        public readonly void Add(KeyValuePair<string, int> item) => _entries.Add(item.Key, item.Value);

        /// <inheritdoc />
        public readonly void Clear() => _entries.Clear();

        /// <inheritdoc />
        public readonly bool Contains(KeyValuePair<string, int> item) => ((ICollection<KeyValuePair<string, int>>)_entries).Contains(item);

        /// <inheritdoc />
        public readonly bool ContainsKey(string key) => _entries.ContainsKey(key);

        /// <inheritdoc />
        public readonly void CopyTo(KeyValuePair<string, int>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, int>>)_entries).CopyTo(array, arrayIndex);

        /// <inheritdoc />
        public readonly IEnumerator<KeyValuePair<string, int>> GetEnumerator() => _entries.GetEnumerator();

        /// <inheritdoc />
        public readonly bool Remove(string key) => _entries.Remove(key);

        /// <inheritdoc />
        public readonly bool Remove(KeyValuePair<string, int> item) => ((ICollection<KeyValuePair<string, int>>)_entries).Remove(item);

        /// <inheritdoc />
        public readonly bool TryGetValue(string key, [MaybeNullWhen(false)] out int value) => _entries.TryGetValue(key, out value);

        /// <inheritdoc />
        readonly IEnumerator IEnumerable.GetEnumerator() => _entries.GetEnumerator();

        #endregion

    }

}
