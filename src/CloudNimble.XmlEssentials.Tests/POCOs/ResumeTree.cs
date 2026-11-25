using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that holds values of its own type, in a member and in a collection.
    /// </summary>
    public sealed class ResumeTree
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the branches.
        /// </summary>
        public List<ResumeTree>? Branches { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the trunk.
        /// </summary>
        public ResumeTree? Trunk { get; set; }

        #endregion

    }

}
