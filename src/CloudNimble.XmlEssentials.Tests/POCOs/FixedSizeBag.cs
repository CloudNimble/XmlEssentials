using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A collection the serializer can write and has no way to create: its only constructor takes an array.
    /// </summary>
    public sealed class FixedSizeBag : IEnumerable<int>
    {

        #region Fields

        private readonly int[] _items;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the bag.
        /// </summary>
        /// <param name="items">The items.</param>
        public FixedSizeBag(int[] items)
        {
            _items = items;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public IEnumerator<int> GetEnumerator()
        {
            return ((IEnumerable<int>)_items).GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        #endregion

    }

}
