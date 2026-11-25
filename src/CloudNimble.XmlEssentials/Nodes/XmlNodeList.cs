using System;
using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// Represents the ordered list of child nodes of an <see cref="XmlElementNode"/>. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// A node can belong to one list at a time. Adding, inserting, or assigning a node that already has a
    /// parent throws an <see cref="XmlException"/>, and <see langword="null"/> throws an
    /// <see cref="ArgumentNullException"/>. A node that is removed or replaced has no parent afterward.
    /// </remarks>
    public sealed class XmlNodeList : IList<XmlNode>
    {

        #region Fields

        private XmlNodeArray<XmlNode> _items;
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
                var old = _items[index];
                if (ReferenceEquals(old, value))
                {
                    return;
                }

                // The new node is checked first. If it is refused, the old one is still in its place.
                Attach(value);
                Detach(old);
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
            foreach (var item in _items.AsSpan())
            {
                Detach(item);
            }

            _items.Clear();
        }

        /// <inheritdoc />
        public bool Contains(XmlNode item)
        {
            return _items.IndexOf(item) >= 0;
        }

        /// <inheritdoc />
        public void CopyTo(XmlNode[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the nodes in order.
        /// </summary>
        /// <returns>
        /// An enumerator for the list.
        /// </returns>
        /// <remarks>
        /// The list must not change while it is enumerated. After a node is added, inserted, removed, or
        /// replaced, or the list is cleared, the next call to <see cref="IEnumerator.MoveNext"/> throws an
        /// <see cref="InvalidOperationException"/>. To change the list in a loop, enumerate a copy of it.
        /// </remarks>
        public IEnumerator<XmlNode> GetEnumerator()
        {
            // The version is taken here, not when the first node is asked for.
            return Enumerate(_items.Version);
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

            // The position is checked before the node is given a parent, so a node that is refused keeps none.
            ArgumentOutOfRangeException.ThrowIfGreaterThan((uint)index, (uint)_items.Count, nameof(index));
            Attach(node);
            _items.Insert(index, node);
        }

        /// <inheritdoc />
        public bool Remove(XmlNode node)
        {
            var index = _items.IndexOf(node);
            if (index < 0)
            {
                return false;
            }

            _items.RemoveAt(index);
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
            // Neither can be content. An attribute among the children would be written as an attribute, and
            // a declaration inside an element is not well formed.
            if (node is XmlAttributeNode)
            {
                throw XmlException.Create("An attribute cannot be a child of an element. Add it to Attributes.");
            }

            if (node is XmlDeclarationNode)
            {
                throw XmlException.Create("An XML declaration cannot be a child of an element.");
            }

            if (node.Parent is not null)
            {
                throw XmlException.Create("The node already has a parent.");
            }

            node.SetParent(_owner);
        }

        /// <summary>
        /// Returns the nodes in order, for a loop that allocates no enumerator.
        /// </summary>
        /// <returns>The nodes.</returns>
        internal ReadOnlySpan<XmlNode> AsSpan()
        {
            return _items.AsSpan();
        }

        /// <summary>
        /// Clears the parent of <paramref name="node"/>.
        /// </summary>
        /// <param name="node">The node.</param>
        internal void Detach(XmlNode node)
        {
            node.SetParent(null);
        }

        /// <summary>
        /// Returns the nodes in order, for as long as the list is as it was when the enumeration started.
        /// </summary>
        /// <param name="version">The version of the list when the enumeration started.</param>
        /// <returns>The nodes.</returns>
        private IEnumerator<XmlNode> Enumerate(int version)
        {
            for (var i = 0; ; i++)
            {
                _items.ThrowIfChanged(version);
                if (i >= _items.Count)
                {
                    yield break;
                }

                yield return _items[i];
            }
        }

        #endregion

    }

}
