using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A read-only dictionary the serializer can write and has no way to create: its only constructor takes a pair.
    /// </summary>
    public sealed class FixedMap : IReadOnlyDictionary<string, int>
    {

        #region Fields

        private readonly Dictionary<string, int> _items;

        #endregion

        #region Properties

        /// <inheritdoc />
        public int Count => _items.Count;

        /// <inheritdoc />
        public IEnumerable<string> Keys => _items.Keys;

        /// <inheritdoc />
        public IEnumerable<int> Values => _items.Values;

        /// <inheritdoc />
        public int this[string key] => _items[key];

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the map with one entry.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public FixedMap(string key, int value)
        {
            _items = new Dictionary<string, int> { [key] = value };
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            return _items.ContainsKey(key);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out int value)
        {
            return _items.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion

    }

}
