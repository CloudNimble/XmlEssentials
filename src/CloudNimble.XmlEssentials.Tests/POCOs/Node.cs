namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A self-referential type used to bind object graphs that contain cycles in the type graph.
    /// </summary>
    public sealed class Node
    {

        #region Public Properties

        /// <summary>
        /// Gets the nested node.
        /// </summary>
        public Node? Child { get; set; }

        /// <summary>
        /// Gets the label.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
