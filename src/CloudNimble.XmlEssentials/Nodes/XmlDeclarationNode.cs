using System;

namespace CloudNimble.Text.Xml.Nodes
{

    /// <summary>
    /// A mutable XML declaration. Immutable documents use <see cref="XmlDeclarationInfo"/> instead.
    /// </summary>
    public sealed class XmlDeclarationNode : XmlNode
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the encoding.
        /// </summary>
        public string Encoding { get; set; } = "utf-8";

        /// <summary>
        /// Gets or sets the standalone flag, or <see langword="null"/> to omit it.
        /// </summary>
        public bool? Standalone { get; set; }

        /// <inheritdoc />
        public override XmlValueKind ValueKind => XmlValueKind.XmlDeclaration;

        /// <summary>
        /// Gets or sets the XML version.
        /// </summary>
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
        }

        #endregion

    }

}
