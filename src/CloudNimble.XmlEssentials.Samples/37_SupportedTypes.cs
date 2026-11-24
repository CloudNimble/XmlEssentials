using System;
using System.Collections.Generic;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Reference;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 37: shows how <see cref="XmlSerializer"/> writes each built-in type and which forms it accepts when it reads.
    /// </summary>
    public static class SupportedTypes
    {

        #region Public Methods

        /// <summary>
        /// Serializes one value of every built-in scalar type and prints the element each one becomes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            Print(output, "bool", true, options);
            Print(output, "byte", (byte)255, options);
            Print(output, "sbyte", (sbyte)-128, options);
            Print(output, "short", (short)-32768, options);
            Print(output, "ushort", (ushort)65535, options);
            Print(output, "int", -42, options);
            Print(output, "uint", 4294967295u, options);
            Print(output, "long", long.MinValue, options);
            Print(output, "ulong", ulong.MaxValue, options);
            Print(output, "float", 3.14f, options);
            Print(output, "double", 1e-7, options);
            Print(output, "double", double.NaN, options);
            Print(output, "double", double.PositiveInfinity, options);
            Print(output, "double", double.NegativeInfinity, options);
            Print(output, "decimal", 19.990m, options);
            Print(output, "string", "Fish & chips", options);
            Print(output, "char", 'A', options);
            Print(output, "Guid", new Guid("0f8fad5b-d9cb-469f-a165-70867728950e"), options);
            Print(output, "DateTime", new DateTime(2026, 11, 23, 14, 30, 0, DateTimeKind.Unspecified), options);
            Print(output, "DateTime", new DateTime(2026, 11, 23, 14, 30, 0, DateTimeKind.Utc), options);
            Print(output, "DateTimeOffset", new DateTimeOffset(2026, 11, 23, 14, 30, 0, TimeSpan.FromHours(2)), options);
            Print(output, "DateOnly", new DateOnly(2026, 11, 23), options);
            Print(output, "TimeOnly", new TimeOnly(14, 30, 0, 250), options);
            Print(output, "TimeSpan", new TimeSpan(1, 2, 3, 4), options);
            Print(output, "byte[]", new byte[] { 1, 2, 3, 4, 5 }, options);
            Print(output, "Uri", new Uri("https://example.com/orders?id=1042&view=full"), options);
            Print(output, "Version", new Version(1, 2, 3), options);
            Print(output, "enum", OrderStatus.Shipped, options);
            Print(output, "[Flags] enum", DeliveryOptions.Gift | DeliveryOptions.Insured, options);
            Print<int?>(output, "int?", 5, options);
            Print<int?>(output, "int?", null, options);
        }

        /// <summary>
        /// Deserializes values written in forms other than the one the serializer writes, and one value that is rejected.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadLexicalForms(TextWriter output)
        {
            output.WriteLine(XmlSerializer.Deserialize<bool>("<Boolean>1</Boolean>"));
            output.WriteLine(XmlSerializer.Deserialize<int>("<Int32> +042 </Int32>"));
            output.WriteLine(double.IsPositiveInfinity(XmlSerializer.Deserialize<double>("<Double>inf</Double>")));
            output.WriteLine(XmlSerializer.Deserialize<Guid>("<Guid>{0F8FAD5B-D9CB-469F-A165-70867728950E}</Guid>"));
            output.WriteLine(XmlSerializer.Deserialize<DateTime>("<DateTime>2026-11-23T14:30:00Z</DateTime>").Kind);
            output.WriteLine(XmlSerializer.Deserialize<TimeSpan>("<TimeSpan>PT90M</TimeSpan>"));
            output.WriteLine(Convert.ToHexString(XmlSerializer.Deserialize<byte[]>("<ByteArray>AQID BAU=</ByteArray>")!));
            output.WriteLine(XmlSerializer.Deserialize<OrderStatus>("<OrderStatus>2</OrderStatus>"));

            try
            {
                XmlSerializer.Deserialize<decimal>("<Decimal>1E+2</Decimal>");
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Writes enums as names and as numbers, and reads a flags value back from its names.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Enums(TextWriter output)
        {
            var names = new XmlSerializerOptions { OmitXmlDeclaration = true };
            var numbers = new XmlSerializerOptions { OmitXmlDeclaration = true, EnumHandling = XmlEnumHandling.Integer };
            var extras = DeliveryOptions.Gift | DeliveryOptions.Insured;

            output.WriteLine(XmlSerializer.Serialize(OrderStatus.Shipped, names));
            output.WriteLine(XmlSerializer.Serialize(OrderStatus.Shipped, numbers));
            output.WriteLine(XmlSerializer.Serialize(extras, names));
            output.WriteLine(XmlSerializer.Serialize(extras, numbers));

            DeliveryOptions read = XmlSerializer.Deserialize<DeliveryOptions>("<DeliveryOptions>Express Insured</DeliveryOptions>");
            output.WriteLine(read == (DeliveryOptions.Express | DeliveryOptions.Insured));
        }

        /// <summary>
        /// Serializes a list, an array, and two dictionaries to show the elements each one produces.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Collections(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            output.WriteLine(XmlSerializer.Serialize(new List<int> { 1, 2 }, options));
            output.WriteLine(XmlSerializer.Serialize(new[] { "red", "green" }, options));
            output.WriteLine(XmlSerializer.Serialize(new Dictionary<string, int> { ["apples"] = 3, ["ripe pears"] = 5 }, options));
            output.WriteLine(XmlSerializer.Serialize(new Dictionary<int, string> { [1042] = "Keyboard" }, options));
        }

        /// <summary>
        /// Shows the exception that reports a type the serializer has no converter for.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void UnsupportedType(TextWriter output)
        {
            try
            {
                XmlSerializer.Serialize(IntPtr.Zero);
            }
            catch (NotSupportedException ex)
            {
                output.WriteLine(ex.Message);
            }

            try
            {
                XmlSerializer.Serialize(new int[2, 2]);
            }
            catch (NotSupportedException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Prints the C# name of a type next to the XML that <paramref name="value"/> is written as.
        /// </summary>
        /// <typeparam name="T">The type to serialize the value as.</typeparam>
        /// <param name="output">The writer that receives the line.</param>
        /// <param name="type">The C# name of the type.</param>
        /// <param name="value">The value to serialize.</param>
        /// <param name="options">The serializer options.</param>
        private static void Print<T>(TextWriter output, string type, T value, XmlSerializerOptions options)
        {
            output.WriteLine($"{type,-15}{XmlSerializer.Serialize(value, options)}");
        }

        #endregion

    }

}
