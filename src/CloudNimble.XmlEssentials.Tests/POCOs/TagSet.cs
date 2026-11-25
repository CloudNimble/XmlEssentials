using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a set member, which is filled through its <c>Add</c> method.
    /// </summary>
    public sealed class TagSet
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the tags.
        /// </summary>
        public HashSet<string>? Tags { get; set; }

        #endregion

    }

}
