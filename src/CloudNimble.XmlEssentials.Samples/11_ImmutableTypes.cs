using System;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Serialization;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 11: deserializes records, types with constructors that take parameters, init-only properties, and structs.
    /// </summary>
    public static class ImmutableTypes
    {

        #region Public Methods

        /// <summary>
        /// Round-trips a <see cref="Temperature"/>, a positional record.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(new Temperature(21.5, "C"), options);
            output.WriteLine(xml);

            Temperature? copy = XmlSerializer.Deserialize<Temperature>(xml, options);
            output.WriteLine(copy == new Temperature(21.5, "C"));
        }

        /// <summary>
        /// Deserializes a <see cref="Measurement"/>, a class whose read-only properties are set by its constructor.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ParameterizedConstructor(TextWriter output)
        {
            const string xml = "<Measurement><Name>Voltage</Name><Value>230</Value></Measurement>";

            Measurement? measurement = XmlSerializer.Deserialize<Measurement>(xml);

            output.WriteLine($"{measurement!.Name} = {measurement.Value}");
        }

        /// <summary>
        /// Shows that the case of a constructor parameter never matters, while the case of an element name matters unless
        /// <see cref="XmlSerializerOptions.PropertyNameCaseInsensitive"/> is set.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ParameterNameCase(TextWriter output)
        {
            const string xml = "<Measurement><name>Voltage</name><value>230</value></Measurement>";
            var options = new XmlSerializerOptions { PropertyNameCaseInsensitive = true };

            Measurement? measurement = XmlSerializer.Deserialize<Measurement>(xml, options);
            output.WriteLine($"{measurement!.Name} = {measurement.Value}");

            try
            {
                XmlSerializer.Deserialize<Measurement>(xml);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Round-trips a <see cref="DateRange"/>, which marks one of its two constructors for deserialization.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ChosenConstructor(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            var week = new DateRange(new DateOnly(2026, 3, 9), days: 7);

            string xml = XmlSerializer.Serialize(week, options);
            output.WriteLine(xml);

            DateRange? copy = XmlSerializer.Deserialize<DateRange>(xml, options);
            output.WriteLine($"{copy!.Start:yyyy-MM-dd} to {copy.End:yyyy-MM-dd}");
        }

        /// <summary>
        /// Deserializes a <see cref="ConnectionSettings"/>, whose properties are init-only.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void InitOnlyProperties(TextWriter output)
        {
            ConnectionSettings? full = XmlSerializer.Deserialize<ConnectionSettings>(
                "<ConnectionSettings><Host>db.contoso.example</Host><Port>6432</Port></ConnectionSettings>");
            output.WriteLine($"{full!.Host}:{full.Port}");

            ConnectionSettings? partial = XmlSerializer.Deserialize<ConnectionSettings>(
                "<ConnectionSettings><Host>db.contoso.example</Host></ConnectionSettings>");
            output.WriteLine($"{partial!.Host}:{partial.Port}");
        }

        /// <summary>
        /// Round-trips a <see cref="GeoPoint"/>, a read-only record struct.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Structs(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(new GeoPoint(47.5, -122.25), options);
            output.WriteLine(xml);

            GeoPoint copy = XmlSerializer.Deserialize<GeoPoint>(xml, options);
            output.WriteLine(copy == new GeoPoint(47.5, -122.25));
        }

        #endregion

    }

}
