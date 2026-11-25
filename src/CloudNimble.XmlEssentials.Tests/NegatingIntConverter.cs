using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// An <see cref="int"/> converter whose output is easy to tell from the built-in one: it writes and reads
    /// the negated value.
    /// </summary>
    public sealed class NegatingIntConverter : XmlConverter<int>
    {

        #region Public Methods

        /// <inheritdoc />
        public override int Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            return -XmlSerializerOptions.Default.GetTypeInfo<int>().Converter.Read(ref reader, typeToConvert, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, int value, XmlSerializerOptions options)
        {
            writer.WriteNumberValue(-value);
        }

        #endregion

    }

}
