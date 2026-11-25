using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// The nodes of a list, held in an array that grows from one place.
    /// </summary>
    /// <typeparam name="T">The node type.</typeparam>
    /// <remarks>
    /// A tree has a list for every element, and most of them hold one node or none. A <c>List&lt;T&gt;</c> is an
    /// object of its own and starts with room for four, which is most of what a small tree weighs. This is a
    /// field of the list that owns it, has no array until a node is added, and then has room for exactly one.
    /// </remarks>
    internal struct XmlNodeArray<T>
        where T : class
    {

        #region Fields

        private int _count;
        private T[]? _items;
        private int _version;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the number of nodes.
        /// </summary>
        internal readonly int Count => _count;

        /// <summary>
        /// Gets or sets the node at <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based position.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not the position of a node.</exception>
        internal T this[int index]
        {
            readonly get
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_count, nameof(index));
                return _items![index];
            }

            set
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_count, nameof(index));
                _items![index] = value;
                _version++;
            }
        }

        /// <summary>
        /// Gets a number that is different after every change to the nodes.
        /// </summary>
        /// <remarks>
        /// An enumeration keeps the number it started with, and stops when the number has moved on
        /// (<see cref="ThrowIfChanged"/>).
        /// </remarks>
        internal readonly int Version => _version;

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds a node at the end.
        /// </summary>
        /// <param name="item">The node.</param>
        internal void Add(T item)
        {
            Insert(_count, item);
        }

        /// <summary>
        /// Returns the nodes, in order.
        /// </summary>
        /// <returns>The nodes.</returns>
        internal readonly ReadOnlySpan<T> AsSpan()
        {
            return new ReadOnlySpan<T>(_items, 0, _count);
        }

        /// <summary>
        /// Removes every node.
        /// </summary>
        internal void Clear()
        {
            _items.AsSpan(0, _count).Clear();
            _count = 0;
            _version++;
        }

        /// <summary>
        /// Copies the nodes into <paramref name="array"/>, starting at <paramref name="arrayIndex"/>.
        /// </summary>
        /// <param name="array">The destination.</param>
        /// <param name="arrayIndex">The position in <paramref name="array"/> of the first node.</param>
        /// <exception cref="ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is negative.</exception>
        /// <exception cref="ArgumentException">The nodes do not fit in <paramref name="array"/> from <paramref name="arrayIndex"/>.</exception>
        internal readonly void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            Array.Copy(_items ?? [], 0, array, arrayIndex, _count);
        }

        /// <summary>
        /// Returns the position of <paramref name="item"/>, or -1.
        /// </summary>
        /// <param name="item">The node to find.</param>
        /// <returns>The zero-based position, or -1 when the node is not here.</returns>
        internal readonly int IndexOf(T item)
        {
            return AsSpan().IndexOf(item);
        }

        /// <summary>
        /// Puts a node at <paramref name="index"/> and moves the nodes from there on up by one.
        /// </summary>
        /// <param name="index">The zero-based position, which may be the count.</param>
        /// <param name="item">The node.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or greater than the count.</exception>
        internal void Insert(int index, T item)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)_count, nameof(index));
            var items = _items ?? [];
            if (_count == items.Length)
            {
                var grown = new T[_count == 0 ? 1 : _count * 2];
                items.AsSpan(0, index).CopyTo(grown);
                items.AsSpan(index, _count - index).CopyTo(grown.AsSpan(index + 1));
                _items = items = grown;
            }
            else
            {
                items.AsSpan(index, _count - index).CopyTo(items.AsSpan(index + 1));
            }

            items[index] = item;
            _count++;
            _version++;
        }

        /// <summary>
        /// Removes the node at <paramref name="index"/> and moves the nodes after it down by one.
        /// </summary>
        /// <param name="index">The zero-based position.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not the position of a node.</exception>
        internal void RemoveAt(int index)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual((uint)index, (uint)_count, nameof(index));
            _count--;
            _items.AsSpan(index + 1, _count - index).CopyTo(_items.AsSpan(index));
            _items![_count] = null!;
            _version++;
        }

        /// <summary>
        /// Stops an enumeration of nodes that have changed since it started.
        /// </summary>
        /// <param name="version">The <see cref="Version"/> the enumeration started with.</param>
        /// <exception cref="InvalidOperationException">The nodes have changed since then.</exception>
        /// <remarks>
        /// This is the rule, and the message, of <c>List&lt;T&gt;</c>, which is what <c>JsonArray</c> enumerates.
        /// </remarks>
        internal readonly void ThrowIfChanged(int version)
        {
            if (version != _version)
            {
                throw new InvalidOperationException("Collection was modified; enumeration operation may not execute.");
            }
        }

        #endregion

    }

}
