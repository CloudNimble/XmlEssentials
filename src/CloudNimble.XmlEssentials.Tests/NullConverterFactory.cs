using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Claims <see cref="uint"/> and then returns null from <see cref="CreateConverter"/>.
    /// </summary>
    public sealed class NullConverterFactory : XmlConverterFactory
    {

        #region Public Methods

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(uint);
        }

        /// <inheritdoc />
        public override XmlConverter? CreateConverter(Type typeToConvert, XmlSerializerOptions options)
        {
            return null;
        }

        #endregion

    }

}
