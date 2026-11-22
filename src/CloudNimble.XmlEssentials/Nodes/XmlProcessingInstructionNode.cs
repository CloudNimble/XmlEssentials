using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// A mutable processing-instruction node.
    /// </summary>
    public sealed class XmlProcessingInstructionNode : XmlNode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the PI data.
        /// </summary>
        public string? Data { get; set; }

        /// <summary>
        /// Gets or sets the PI target.
        /// </summary>
        public string Target { get; set; }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.ProcessingInstruction;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a processing instruction.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="data">The data.</param>
        public XmlProcessingInstructionNode(string target, string? data = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(target);
            Target = target;
            Data = data;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override XmlNode DeepClone()
        {
            return new XmlProcessingInstructionNode(Target, Data);
        }

        /// <inheritdoc />
        public override void WriteTo(Utf8XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteProcessingInstruction(Target, Data);
        }

        #endregion

    }

}
