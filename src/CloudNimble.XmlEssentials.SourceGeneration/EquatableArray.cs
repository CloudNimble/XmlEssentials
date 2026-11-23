using System;
using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// An array that compares by sequence equality for incremental generator caching.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
        where T : IEquatable<T>
    {

        #region Fields

        private readonly T[] _values;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the number of items.
        /// </summary>
        internal int Length => _values.Length;

        /// <summary>
        /// Gets the item at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index.</param>
        internal T this[int index] => _values[index];

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the array.
        /// </summary>
        /// <param name="values">The items.</param>
        internal EquatableArray(T[] values)
        {
            _values = values;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Equals(EquatableArray<T> other)
        {
            if (ReferenceEquals(_values, other._values))
            {
                return true;
            }

            if (_values.Length != other._values.Length)
            {
                return false;
            }

            for (var i = 0; i < _values.Length; i++)
            {
                if (!_values[i].Equals(other._values[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is EquatableArray<T> other && Equals(other);
        }

        /// <inheritdoc />
        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)_values).GetEnumerator();
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var hash = 17;
            foreach (var value in _values)
            {
                hash = (hash * 31) + value.GetHashCode();
            }

            return hash;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

    }

}
