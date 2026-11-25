using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A collection that is not an <see cref="ICollection{T}"/> and is filled through its <c>Add</c> method.
    /// </summary>
    public sealed class AddOnlyBag : IEnumerable<int>
    {

        #region Fields

        private readonly List<int> _items = [];

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(int item)
        {
            _items.Add(item);
        }

        /// <inheritdoc />
        public IEnumerator<int> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion

    }

}
