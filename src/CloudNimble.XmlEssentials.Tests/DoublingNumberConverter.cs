using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Writes twice the stored integer.
    /// </summary>
    public sealed class DoublingNumberConverter : XmlConverter<DoublingNumber>
    {

        #region Public Methods

        /// <inheritdoc />
        public override DoublingNumber Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            return new DoublingNumber { Value = XmlInt32Converter.Instance.Read(ref reader, typeof(int), options) / 2 };
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, DoublingNumber value, XmlSerializerOptions options)
        {
            writer.WriteNumberValue(value.Value * 2);
        }

        #endregion

    }

}
