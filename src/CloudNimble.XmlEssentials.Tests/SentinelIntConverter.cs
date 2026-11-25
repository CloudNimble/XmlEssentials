using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// An <see cref="int"/> converter that asks to see nil elements and reads them as -1.
    /// </summary>
    public sealed class SentinelIntConverter : XmlConverter<int>
    {

        #region Public Properties

        /// <inheritdoc />
        public override bool HandleNull => true;

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override int Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            if (reader.GetNil())
            {
                reader.Skip();
                return -1;
            }

            return XmlSerializerOptions.Default.GetTypeInfo<int>().Converter.Read(ref reader, typeToConvert, options);
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, int value, XmlSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }

        #endregion

    }

}
