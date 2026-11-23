using System;
using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// Represents the ordered list of attributes of an <see cref="XmlElementNode"/>. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// An attribute can belong to one list at a time. Adding, inserting, or assigning an attribute that
    /// already has a parent throws an <see cref="XmlException"/>, and <see langword="null"/> throws an
    /// <see cref="ArgumentNullException"/>. An attribute that is removed or replaced has no parent afterward.
    /// The list does not check for duplicate names.
    /// </remarks>
    public sealed class XmlAttributeNodeList : IList<XmlAttributeNode>
    {

        #region Fields

        private readonly List<XmlAttributeNode> _items = [];
        private readonly XmlNode _owner;

        #endregion

        #region Public Properties

        /// <inheritdoc />
        public int Count => _items.Count;

        /// <inheritdoc />
        public bool IsReadOnly => false;

        /// <inheritdoc />
        public XmlAttributeNode this[int index]
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

        /// <summary>
        /// Gets the first attribute that has the specified local name, in any namespace.
        /// </summary>
        /// <param name="localName">The local name of the attribute to find, without a prefix.</param>
        /// <value>
        /// The first attribute that has the specified local name, or <see langword="null"/> if there is none.
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="localName"/> is <see langword="null"/>.
        /// </exception>
        public XmlAttributeNode? this[string localName]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(localName);
                foreach (var item in _items)
                {
                    if (item.LocalName == localName)
                    {
                        return item;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the first attribute that has the specified local name and namespace URI.
        /// </summary>
        /// <param name="localName">The local name of the attribute to find, without a prefix.</param>
        /// <param name="namespaceUri">The namespace URI of the attribute, or an empty string for an attribute in no namespace.</param>
        /// <value>
        /// The first attribute that has the specified local name and namespace URI, or
        /// <see langword="null"/> if there is none.
        /// </value>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="localName"/> or <paramref name="namespaceUri"/> is <see langword="null"/>.
        /// </exception>
        public XmlAttributeNode? this[string localName, string namespaceUri]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(localName);
                ArgumentNullException.ThrowIfNull(namespaceUri);
                foreach (var item in _items)
                {
                    if (item.LocalName == localName && item.NamespaceUri == namespaceUri)
                    {
                        return item;
                    }
                }

                return null;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a list owned by <paramref name="owner"/>.
        /// </summary>
        /// <param name="owner">The parent element.</param>
        internal XmlAttributeNodeList(XmlNode owner)
        {
            _owner = owner;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void Add(XmlAttributeNode node)
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
        public bool Contains(XmlAttributeNode item)
        {
            return _items.Contains(item);
        }

        /// <inheritdoc />
        public void CopyTo(XmlAttributeNode[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public IEnumerator<XmlAttributeNode> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <inheritdoc />
        public int IndexOf(XmlAttributeNode item)
        {
            return _items.IndexOf(item);
        }

        /// <inheritdoc />
        public void Insert(int index, XmlAttributeNode node)
        {
            ArgumentNullException.ThrowIfNull(node);
            Attach(node);
            _items.Insert(index, node);
        }

        /// <inheritdoc />
        public bool Remove(XmlAttributeNode node)
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
        internal void Attach(XmlAttributeNode node)
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
        internal void Detach(XmlAttributeNode node)
        {
            node.SetParent(null);
        }

        #endregion

    }

}
