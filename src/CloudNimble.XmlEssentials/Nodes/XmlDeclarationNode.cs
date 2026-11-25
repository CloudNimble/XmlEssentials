using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// Represents a mutable XML declaration. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// The read-only <see cref="XmlDocument"/> reports its declaration as an <see cref="XmlDeclarationInfo"/> instead.
    /// <see cref="WriteTo(Utf8XmlWriter)"/> writes <see cref="Version"/> and <see cref="Encoding"/> as they
    /// are, without validating them.
    /// </remarks>
    public sealed class XmlDeclarationNode : XmlNode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the encoding name that the declaration states.
        /// </summary>
        /// <value>
        /// The value of the <c>encoding</c> pseudo-attribute. The default is <c>utf-8</c>.
        /// </value>
        public string Encoding { get; set; } = "utf-8";

        /// <summary>
        /// Gets or sets the standalone document declaration.
        /// </summary>
        /// <value>
        /// <see langword="true"/> to write <c>standalone="yes"</c>; <see langword="false"/> to write
        /// <c>standalone="no"</c>; <see langword="null"/> to omit the <c>standalone</c> pseudo-attribute.
        /// The default is <see langword="null"/>.
        /// </value>
        public bool? Standalone { get; set; }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.XmlDeclaration;

        /// <summary>
        /// Gets or sets the XML version that the declaration states.
        /// </summary>
        /// <value>
        /// The value of the <c>version</c> pseudo-attribute. The default is <c>1.0</c>.
        /// </value>
        public string Version { get; set; } = "1.0";

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override XmlNode DeepClone()
        {
            return new XmlDeclarationNode
            {
                Version = Version,
                Encoding = Encoding,
                Standalone = Standalone,
            };
        }

        /// <inheritdoc />
        public override void WriteTo(Utf8XmlWriter writer)
        {
            ArgumentNullException.ThrowIfNull(writer);
            var standalone = Standalone is { } flag
                ? flag ? " standalone=\"yes\"" : " standalone=\"no\""
                : string.Empty;
            writer.WriteRawValue($"<?xml version=\"{Version}\" encoding=\"{Encoding}\"{standalone}?>", skipInputValidation: true);

            // The writer would otherwise write a declaration of its own before the first element.
            writer.MarkDeclarationWritten();
        }

        #endregion

    }

}
