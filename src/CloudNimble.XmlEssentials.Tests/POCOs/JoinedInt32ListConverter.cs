using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Converts a list of numbers as one value, with the numbers separated by commas.
    /// </summary>
    public sealed class JoinedInt32ListConverter : XmlConverter<List<int>>
    {

        #region Public Methods

        /// <inheritdoc />
        public override List<int>? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var value = new List<int>();
            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                value.AddRange(reader.GetString()!.Split(',').Select(part => int.Parse(part, CultureInfo.InvariantCulture)));
            }

            return value;
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, List<int> value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(string.Join(",", value));
        }

        #endregion

    }

}
