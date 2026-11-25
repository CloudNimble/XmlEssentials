namespace CloudNimble.XmlEssentials.Tests.POCOs.Paths
{

    /// <summary>
    /// A node that holds the next node, to make a document as deep as a test needs.
    /// </summary>
    public sealed class PathNode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a label, which the writer refuses when it holds a character XML cannot carry.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Gets or sets the next node.
        /// </summary>
        public PathNode? Next { get; set; }

        #endregion

    }

}
