using System;
using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// An ordered list of child <see cref="XmlNode"/> instances.
    /// </summary>
    public sealed class XmlNodeList : IList<XmlNode>
    {

        #region Fields

        private readonly List<XmlNode> _items = [];
        private readonly XmlNode _owner;

        #endregion

        #region Public Properties

        /// <inheritdoc />
        public int Count => _items.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public XmlNode this[int index]
        {
            get => _items[index];
            set
            {
                ArgumentNullException.ThrowIfNull(value);
                Detach(_items[index]);
                Attach(value);
                _items[index] = value;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a list owned by <paramref name="owner"/>.
        /// </summary>
        /// <param name="owner">The parent element.</param>
        internal XmlNodeList(XmlNode owner)
        {
            _owner = owner;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void Add(XmlNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            Attach(node);
            _items.Add(node);
        }

        /// <inheritdoc />
        public void Clear()
        {
            foreach (var item in _items)
            {
                Detach(item);
            }

            _items.Clear();
        }

        /// <inheritdoc />
        public bool Contains(XmlNode item)
        {
            return _items.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(XmlNode[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public IEnumerator<XmlNode> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <inheritdoc />
        public int IndexOf(XmlNode item)
        {
            return _items.IndexOf(item);
        }

        /// <inheritdoc />
        public void Insert(int index, XmlNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            Attach(node);
            _items.Insert(index, node);
        }

        /// <inheritdoc />
        public bool Remove(XmlNode node)
        {
            if (!_items.Remove(node))
            {
                return false;
            }

            Detach(node);
            return true;
        }

        /// <inheritdoc />
        public void RemoveAt(int index)
        {
            Detach(_items[index]);
            _items.RemoveAt(index);
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Assigns <paramref name="node"/> to this owner.
        /// </summary>
        /// <param name="node">The node.</param>
        internal void Attach(XmlNode node)
        {
            if (node.Parent is not null)
            {
                throw new XmlException("The node already has a parent.");
            }

            node.SetParent(_owner);
        }

        /// <summary>
        /// Clears the parent of <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node.</param>
        internal void Detach(XmlNode node)
        {
            node.SetParent(null);
        }

        #endregion

    }

}
