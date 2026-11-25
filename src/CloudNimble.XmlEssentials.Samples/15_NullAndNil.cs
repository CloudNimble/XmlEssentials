using System;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Samples.Models.Shapes;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 15: writes and reads <see langword="null"/> values.
    /// </summary>
    public static class NullAndNil
    {

        #region Public Methods

        /// <summary>
        /// Serializes null members with the default handling, which marks each one with <c>xsi:nil</c>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };
            var contact = new Contact { Name = "Ada", Nickname = null, Age = null };

            string xml = XmlSerializer.Serialize(contact, options);
            output.WriteLine(xml);

            Contact? copy = XmlSerializer.Deserialize<Contact>(xml, options);
            output.WriteLine($"Nickname is null: {copy!.Nickname is null}");
            output.WriteLine($"Age is null: {copy.Age is null}");
        }

        /// <summary>
        /// Serializes an enumeration member as its name and as its number.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void EnumValues(TextWriter output)
        {
            var ticket = new Ticket { Id = 311, Priority = TicketPriority.High };

            var byName = new XmlSerializerOptions { OmitXmlDeclaration = true };
            output.WriteLine(XmlSerializer.Serialize(ticket, byName));

            var byNumber = new XmlSerializerOptions { OmitXmlDeclaration = true, EnumHandling = XmlEnumHandling.Integer };
            string xml = XmlSerializer.Serialize(ticket, byNumber);
            output.WriteLine(xml);

            Ticket? copy = XmlSerializer.Deserialize<Ticket>(xml);
            output.WriteLine(copy!.Priority);
        }

        /// <summary>
        /// Leaves null members out of the document with <see cref="XmlIgnoreCondition.WhenWritingNull"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void OmitNulls(TextWriter output)
        {
            var options = new XmlSerializerOptions
            {
                DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull,
                WriteIndented = true,
                NewLine = "\n",
            };

            var contact = new Contact { Name = "Ada", Nickname = null, Age = null };
            output.WriteLine(XmlSerializer.Serialize(contact, options));
        }

        /// <summary>
        /// Reads the three shapes a document can give a member: left out, empty, and nil.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadShapes(TextWriter output)
        {
            Contact? omitted = XmlSerializer.Deserialize<Contact>(
                "<Contact><Name>Ada</Name></Contact>");
            output.WriteLine($"Omitted: {Describe(omitted!.Nickname)}");

            Contact? empty = XmlSerializer.Deserialize<Contact>(
                "<Contact><Name>Ada</Name><Nickname /></Contact>");
            output.WriteLine($"Empty: {Describe(empty!.Nickname)}");

            Contact? nil = XmlSerializer.Deserialize<Contact>(
                """<Contact xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"><Name>Ada</Name><Nickname xsi:nil="true" /></Contact>""");
            output.WriteLine($"Nil: {Describe(nil!.Nickname)}");

            try
            {
                XmlSerializer.Deserialize<Contact>("<Contact><Name>Ada</Name><Age /></Contact>");
            }
            catch (XmlException ex)
            {
                output.WriteLine($"Empty number: {ex.Message}");
            }
        }

        /// <summary>
        /// Serializes a null reference as the document root.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteNullRoot(TextWriter output)
        {
            Contact? nobody = null;

            string xml = XmlSerializer.Serialize(nobody);
            output.WriteLine(xml);
            output.WriteLine(XmlSerializer.Deserialize<Contact>(xml) is null);

            // An ignore condition is about members. The document still needs its root, so a null root is written.
            var omitNulls = new XmlSerializerOptions { DefaultIgnoreCondition = XmlIgnoreCondition.WhenWritingNull };
            output.WriteLine(XmlSerializer.Serialize(nobody, omitNulls) == xml);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Describes a string so that <see langword="null"/> and the empty string can be told apart in the output.
        /// </summary>
        /// <param name="value">The string to describe.</param>
        /// <returns><c>null</c>, <c>empty string</c>, or the string in quotation marks.</returns>
        private static string Describe(string? value)
        {
            return value switch
            {
                null => "null",
                "" => "empty string",
                _ => $"\"{value}\"",
            };
        }

        #endregion

    }

}
