using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A collection that is a struct and is filled through its <c>Add</c> method.
    /// </summary>
    public struct StructBag : IEnumerable<int>
    {

        #region Fields

        private List<int>? _items;

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds an item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(int item)
        {
            (_items ??= []).Add(item);
        }

        /// <inheritdoc />
        public readonly IEnumerator<int> GetEnumerator()
        {
            return (_items ?? []).GetEnumerator();
        }

        /// <inheritdoc />
        readonly IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

    }

}
