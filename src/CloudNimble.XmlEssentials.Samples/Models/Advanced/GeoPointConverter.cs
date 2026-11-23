using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// Converts <see cref="GeoPoint"/> to and from a <c>Lat</c> and a <c>Lon</c> child element.
    /// </summary>
    public sealed class GeoPointConverter : XmlConverter<GeoPoint>
    {

        #region Public Methods

        /// <inheritdoc />
        public override GeoPoint Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            double latitude = 0;
            double longitude = 0;
            if (reader.IsEmptyElement)
            {
                return new GeoPoint(latitude, longitude);
            }

            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                if (reader.TokenType is not XmlTokenType.StartElement)
                {
                    continue;
                }

                if (reader.LocalNameEquals("Lat"u8))
                {
                    latitude = ReadNumber(ref reader);
                }
                else if (reader.LocalNameEquals("Lon"u8))
                {
                    longitude = ReadNumber(ref reader);
                }
                else
                {
                    reader.Skip();
                }
            }

            return new GeoPoint(latitude, longitude);
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, GeoPoint value, XmlSerializerOptions options)
        {
            writer.WriteStartElement("Lat");
            writer.WriteNumberValue(value.Latitude);
            writer.WriteEndElement();

            writer.WriteStartElement("Lon");
            writer.WriteNumberValue(value.Longitude);
            writer.WriteEndElement();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads the number in the element the reader is on and leaves the reader on that element's end tag.
        /// </summary>
        /// <param name="reader">The reader, positioned on the start tag of the element.</param>
        /// <returns>The number, or zero when the element is empty.</returns>
        private static double ReadNumber(ref Utf8XmlReader reader)
        {
            double value = 0;
            if (reader.IsEmptyElement)
            {
                return value;
            }

            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                if (reader.TokenType is XmlTokenType.Text)
                {
                    value = reader.GetDouble();
                }
            }

            return value;
        }

        #endregion

    }

}
