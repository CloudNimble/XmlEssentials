using System;
using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// A list owned by an <see cref="XmlSerializerOptions"/>. It rejects null items, rejects every change once
    /// the options are read-only, and tells the options when it changes.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    internal abstract class XmlConfigurationList<T> : IList<T>
        where T : class
    {

        #region Fields

        private readonly List<T> _items = [];

        #endregion

        #region Public Properties

        /// <inheritdoc />
        public int Count => _items.Count;

        /// <inheritdoc />
        public bool IsReadOnly => Options.IsReadOnly;

        /// <inheritdoc />
        public T this[int index]
        {
            get => _items[index];
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                Options.VerifyMutable();
                _items[index] = value;
                OnChanged();
            }
        }

        /// <summary>
        /// Gets the options that own this list.
        /// </summary>
        internal XmlSerializerOptions Options { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty list owned by <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The owner.</param>
        private protected XmlConfigurationList(XmlSerializerOptions options)
        {
            Options = options;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void Add(T item)
        {
            Insert(_items.Count, item);
        }

        /// <inheritdoc />
        public void Clear()
        {
            Options.VerifyMutable();
            _items.Clear();
            OnChanged();
        }

        /// <inheritdoc />
        public bool Contains(T item)
        {
            return _items.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(T[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that does not allocate.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public List<T>.Enumerator GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <inheritdoc />
        public int IndexOf(T item)
        {
            return _items.IndexOf(item);
        }

        /// <inheritdoc />
        public void Insert(int index, T item)
        {
            ArgumentNullException.ThrowIfNull(item);
            Options.VerifyMutable();
            _items.Insert(index, item);
            OnChanged();
        }

        /// <inheritdoc />
        public bool Remove(T item)
        {
            var index = _items.IndexOf(item);
            if (index < 0)
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            Options.VerifyMutable();
            _items.RemoveAt(index);
            OnChanged();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called after the list changes.
        /// </summary>
        private protected abstract void OnChanged();

        /// <summary>
        /// Replaces the content without the read-only check and without <see cref="OnChanged"/>. For the owner
        /// to seed or resynchronize the list.
        /// </summary>
        /// <param name="items">The new content.</param>
        private protected void ReplaceAll(IEnumerable<T> items)
        {
            if (!ReferenceEquals(items, this))
            {
                _items.Clear();
                _items.AddRange(items);
            }
        }

        #endregion

    }

}
