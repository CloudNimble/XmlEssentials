using System.Collections;
using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A collection of <see cref="long"/> that no generated context names and no other test uses, so that what is
    /// known about it is only what a test of the collection shapes puts there.
    /// </summary>
    public sealed class UnnamedBag : IEnumerable<long>
    {

        #region Public Methods

        /// <inheritdoc />
        public IEnumerator<long> GetEnumerator()
        {
            yield break;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

    }

}
