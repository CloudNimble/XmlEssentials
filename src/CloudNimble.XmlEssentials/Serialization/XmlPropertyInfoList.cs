using System;
using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The list of members that <see cref="XmlTypeInfo.Properties"/> returns.
    /// </summary>
    /// <remarks>
    /// The type info that owns the list is read-only, so the list is too: every member that would change it
    /// throws an <see cref="InvalidOperationException"/>, as the list of a read-only <c>JsonTypeInfo</c> does in
    /// <c>System.Text.Json</c>. An argument is checked before that, in the same order.
    /// </remarks>
    internal sealed class XmlPropertyInfoList : IList<XmlPropertyInfo>
    {

        #region Fields

        /// <summary>
        /// The list of a type that has no members to describe.
        /// </summary>
        internal static readonly XmlPropertyInfoList Empty = new([]);

        /// <summary>
        /// The message of the exception a change to a read-only type info throws.
        /// </summary>
        internal const string ReadOnlyMessage = "This XmlTypeInfo instance is marked read-only or has already been used in serialization or deserialization.";

        private readonly XmlPropertyInfo[] _items;

        #endregion

        #region Public Properties

        /// <inheritdoc />
        public int Count => _items.Length;

        /// <inheritdoc />
        public bool IsReadOnly => true;

        /// <inheritdoc />
        public XmlPropertyInfo this[int index]
        {
            get => _items[index];
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                throw new InvalidOperationException(ReadOnlyMessage);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the list.
        /// </summary>
        /// <param name="items">The members, in the order they are written. The list keeps the array.</param>
        internal XmlPropertyInfoList(XmlPropertyInfo[] items)
        {
            _items = items;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void Add(XmlPropertyInfo item)
        {
            ArgumentNullException.ThrowIfNull(item);
            throw new InvalidOperationException(ReadOnlyMessage);
        }

        /// <inheritdoc />
        public void Clear()
        {
            throw new InvalidOperationException(ReadOnlyMessage);
        }

        /// <inheritdoc />
        public bool Contains(XmlPropertyInfo item)
        {
            return Array.IndexOf(_items, item) >= 0;
        }

        /// <inheritdoc />
        public void CopyTo(XmlPropertyInfo[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public IEnumerator<XmlPropertyInfo> GetEnumerator()
        {
            return ((IEnumerable<XmlPropertyInfo>)_items).GetEnumerator();
        }

        /// <inheritdoc />
        public int IndexOf(XmlPropertyInfo item)
        {
            return Array.IndexOf(_items, item);
        }

        /// <inheritdoc />
        public void Insert(int index, XmlPropertyInfo item)
        {
            ArgumentNullException.ThrowIfNull(item);
            throw new InvalidOperationException(ReadOnlyMessage);
        }

        /// <inheritdoc />
        public bool Remove(XmlPropertyInfo item)
        {
            throw new InvalidOperationException(ReadOnlyMessage);
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            throw new InvalidOperationException(ReadOnlyMessage);
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion

    }

}
