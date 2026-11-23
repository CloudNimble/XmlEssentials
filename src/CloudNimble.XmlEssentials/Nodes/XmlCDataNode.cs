using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// Represents a mutable XML CDATA section. This class cannot be inherited.
    /// </summary>
    public sealed class XmlCDataNode : XmlNode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the content of this CDATA section.
        /// </summary>
        /// <value>
        /// The text between <c>&lt;![CDATA[</c> and <c>]]&gt;</c>.
        /// </value>
        public string Value { get; set; }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.CData;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlCDataNode"/> class with the specified content.
        /// </summary>
        /// <param name="value">The content of the CDATA section. The default is <see langword="null"/>, which is stored as an empty string.</param>
        public XmlCDataNode(string? value = null)
        {
            Value = value ?? string.Empty;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override XmlNode DeepClone()
        {
            return new XmlCDataNode(Value);
        }

        /// <inheritdoc />
        public override void WriteTo(Utf8XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            writer.WriteCData(Value);
        }

        #endregion

    }

}
