using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A factory for <see cref="uint"/> that wrongly returns another factory instead of a converter.
    /// </summary>
    public sealed class FactoryReturningFactory : XmlConverterFactory
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
            return new NullConverterFactory();
        }

        #endregion

    }

}
