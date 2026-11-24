using System;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts an <see cref="XmlNode"/> or an <see cref="XmlElementNode"/> as the element it is.
    /// </summary>
    /// <typeparam name="TNode">The declared node type.</typeparam>
    /// <remarks>
    /// Only an element can be the value. Text, a comment, or a processing instruction inside a member's
    /// element would not be read back as a node, so it is refused when it is written.
    /// </remarks>
    internal sealed class XmlNodeFragmentConverter<TNode> : XmlFragmentConverter<TNode>
        where TNode : XmlNode
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlNodeFragmentConverter<TNode> Instance = new();

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override TNode ReadValue(ref Utf8XmlReader reader)
        {
            // A reader on a start tag always gives back an element node, which is both declared types.
            return (TNode)XmlNode.ParseValue(ref reader);
        }

        /// <inheritdoc />
        internal override void WriteValue(Utf8XmlWriter writer, TNode value)
        {
            if (value is not XmlElementNode)
            {
                throw new NotSupportedException($"Only an element can be serialized as an XML value. The node is a '{value.GetType()}'.");
            }

            value.WriteTo(writer);
        }

        #endregion

    }

}
