using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// A mutable CDATA node.
    /// </summary>
    public sealed class XmlCDataNode : XmlNode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the CDATA payload.
        /// </summary>
        public string Value { get; set; }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.CData;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a CDATA node.
        /// </summary>
        /// <param name="value">The payload.</param>
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
