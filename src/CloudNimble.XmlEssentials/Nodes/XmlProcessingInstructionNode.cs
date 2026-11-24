using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// Represents a mutable XML processing instruction. This class cannot be inherited.
    /// </summary>
    public sealed class XmlProcessingInstructionNode : XmlNode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the data of this processing instruction.
        /// </summary>
        /// <value>
        /// The text that follows the target, or <see langword="null"/> if the processing instruction has no data.
        /// </value>
        public string? Data { get; set; }

        /// <summary>
        /// Gets or sets the target of this processing instruction.
        /// </summary>
        /// <value>
        /// The name that identifies the application to which the instruction is directed.
        /// </value>
        /// <remarks>
        /// A target that is <see langword="null"/>, empty, or white space is rejected, by the constructor and when the property is set.
        /// </remarks>
        public string Target
        {
            get;
            set
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(value);
                field = value;
            }
        }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.ProcessingInstruction;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlProcessingInstructionNode"/> class with the specified target and data.
        /// </summary>
        /// <param name="target">The target of the processing instruction.</param>
        /// <param name="data">The data of the processing instruction, or <see langword="null"/> for none. The default is <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="target"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="target"/> is empty or consists only of white-space characters.
        /// </exception>
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
