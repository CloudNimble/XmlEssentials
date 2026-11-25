using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// An <see cref="int"/> converter that wrongly claims it can convert <see cref="uint"/>.
    /// </summary>
    public sealed class UIntClaimingConverter : XmlConverter<int>
    {

        #region Public Methods

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(uint);
        }

        /// <inheritdoc />
        public override int Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            return 0;
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, int value, XmlSerializerOptions options)
        {
        }

        #endregion

    }

}
