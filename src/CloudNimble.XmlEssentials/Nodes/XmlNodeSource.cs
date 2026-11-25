namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// The parsed document a tree of nodes was made from, which the tree keeps so that an element's attributes
    /// and children are made only when they are first asked for.
    /// </summary>
    /// <remarks>
    /// This is how <c>JsonNode.Parse</c> works in System.Text.Json: the parsed value is kept, and the nodes
    /// under a node are created when something first looks at them. The document holds no pooled memory, since
    /// nothing disposes it; it lives as long as a node that has not been looked into.
    /// </remarks>
    internal sealed class XmlNodeSource
    {

        #region Fields

        /// <summary>
        /// The parsed document.
        /// </summary>
        internal readonly XmlDocument Document;

        /// <summary>
        /// The name strings made so far, so that every element of the same name shares one string
        /// (<see cref="XmlDocument.NameString"/>).
        /// </summary>
        internal readonly string?[] Names = new string?[64];

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the source of a tree.
        /// </summary>
        /// <param name="document">The parsed document, which holds no pooled memory.</param>
        internal XmlNodeSource(XmlDocument document)
        {
            Document = document;
        }

        #endregion

    }

}
