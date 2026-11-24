using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Paths
{

    /// <summary>
    /// A converter that throws an <see cref="XmlException"/> with a message of its own, and with a path of its
    /// own when the element it reads has a <c>path</c> attribute.
    /// </summary>
    public sealed class PathOddConverter : XmlConverter<PathOdd>
    {

        #region Public Methods

        /// <inheritdoc />
        public override PathOdd? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            if (reader.MoveToNextAttribute())
            {
                throw new XmlException("mine", bytePosition: 1, lineNumber: 2, linePosition: 3, path: reader.GetString());
            }

            throw new XmlException("mine");
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, PathOdd value, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(value);
            throw value.GivenPath is null ? new XmlException("mine") : new XmlException("mine", null, null, null, value.GivenPath);
        }

        #endregion

    }

}
