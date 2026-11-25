using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A collection type that cannot be created because it is abstract.
    /// </summary>
    public abstract class AbstractBag : IEnumerable<int>
    {

        #region Public Methods

        /// <inheritdoc />
        public abstract IEnumerator<int> GetEnumerator();

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

    }

}
