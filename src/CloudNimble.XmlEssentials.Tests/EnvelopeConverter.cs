using System;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Writes an <see cref="Envelope"/> as the documents its content serializes to, each produced by calling
    /// <see cref="XmlSerializer"/> again while the outer call is still writing.
    /// </summary>
    public sealed class EnvelopeConverter : XmlConverter<Envelope>
    {

        #region Public Methods

        /// <inheritdoc />
        public override Envelope Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var inner = options.GetTypeInfo<string>().Converter.Read(ref reader, typeof(string), options)!;
            return new Envelope { Content = XmlSerializer.Deserialize<int>(inner.Split('|')[0], options) };
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, Envelope value, XmlSerializerOptions options)
        {
            using var stream = new MemoryStream();
            XmlSerializer.Serialize(stream, value.Content, options);
            writer.WriteStringValue(XmlSerializer.Serialize(value.Content, options));
            writer.WriteStringValue("|"u8);
            writer.WriteStringValue(stream.ToArray());
        }

        #endregion

    }

}
