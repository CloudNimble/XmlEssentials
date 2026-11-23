using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models;
using CloudNimble.XmlEssentials.Samples.Models.Reference;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 39: shows what the library rejects in untrusted XML, and how to share options between threads.
    /// </summary>
    public static class SecurityAndThreadSafety
    {

        #region Public Methods

        /// <summary>
        /// Gives the serializer and the document parser two documents that carry a DTD: an entity expansion
        /// attack and an external entity.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            const string entityExpansion = """
                <?xml version="1.0"?>
                <!DOCTYPE Person [
                  <!ENTITY a "aaaaaaaaaa">
                  <!ENTITY b "&a;&a;&a;&a;&a;&a;&a;&a;&a;&a;">
                  <!ENTITY c "&b;&b;&b;&b;&b;&b;&b;&b;&b;&b;">
                ]>
                <Person><Name>&c;</Name><Age>36</Age></Person>
                """;

            try
            {
                XmlSerializer.Deserialize<Person>(entityExpansion);
            }
            catch (XmlException ex)
            {
                output.WriteLine($"Line {ex.LineNumber}: {ex.Message}");
            }

            const string externalEntity = """
                <!DOCTYPE Person [<!ENTITY secret SYSTEM "file:///etc/passwd">]>
                <Person><Name>&secret;</Name></Person>
                """;

            try
            {
                using XmlDocument document = XmlDocument.Parse(externalEntity, new XmlDocumentOptions());
            }
            catch (XmlException ex)
            {
                output.WriteLine($"Line {ex.LineNumber}: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads documents that exceed the default depth limit and two limits that the caller lowered.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void EnforceLimits(TextWriter output)
        {
            string deep = string.Concat(Enumerable.Repeat("<a>", 65)) + string.Concat(Enumerable.Repeat("</a>", 65));

            try
            {
                using XmlDocument document = XmlDocument.Parse(deep, new XmlDocumentOptions());
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }

            var options = new XmlSerializerOptions
            {
                MaxDepth = 8,
                MaxAttributeCount = 16,
                MaxNameByteCount = 64,
                MaxTextByteCount = 256,
            };
            string longName = new('x', 300);

            try
            {
                XmlSerializer.Deserialize<Person>($"<Person><Name>{longName}</Name></Person>", options);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }

            try
            {
                XmlSerializer.Deserialize<Person>($"<Person><{longName}>Ada</{longName}></Person>", options);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Deserializes from a stream that holds more bytes than <see cref="XmlSerializerOptions.MaxDocumentByteCount"/> allows.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void LimitDocumentSize(TextWriter output)
        {
            var options = new XmlSerializerOptions { MaxDocumentByteCount = 1024 };
            string xml = $"<Person><Name>{new string('x', 4000)}</Name></Person>";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

            try
            {
                XmlSerializer.Deserialize<Person>(stream, options);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Deserializes a polymorphic member whose document names a declared type, and then a .NET type name.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RejectUndeclaredTypes(TextWriter output)
        {
            const string declared = """
                <Delivery>
                  <Carrier xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:type="courier">
                    <Name>Swift</Name>
                    <Vehicle>Bicycle</Vehicle>
                  </Carrier>
                  <OrderId>1042</OrderId>
                </Delivery>
                """;

            Delivery? delivery = XmlSerializer.Deserialize<Delivery>(declared);
            output.WriteLine(delivery!.Carrier!.GetType().Name);

            const string undeclared = """
                <Delivery>
                  <Carrier xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xsi:type="System.Diagnostics.Process, System">
                    <Name>Swift</Name>
                  </Carrier>
                  <OrderId>1042</OrderId>
                </Delivery>
                """;

            try
            {
                XmlSerializer.Deserialize<Delivery>(undeclared);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Serializes on several threads at once with one <see cref="XmlSerializerOptions"/> instance.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ShareOptions(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            var results = new string[4];

            Parallel.For(0, results.Length, i =>
            {
                var person = new Person { Name = $"Worker {i}", Age = 30 + i };
                results[i] = XmlSerializer.Serialize(person, options);
            });

            foreach (string xml in results)
            {
                output.WriteLine(xml);
            }
        }

        /// <summary>
        /// Shows that options become read-only on first use, and how to derive changed options from them.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void FreezeOptions(TextWriter output)
        {
            var person = new Person { Name = "Ada", Age = 36 };
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            output.WriteLine($"Before first use: IsReadOnly = {options.IsReadOnly}");

            XmlSerializer.Serialize(person, options);
            output.WriteLine($"After first use:  IsReadOnly = {options.IsReadOnly}");

            try
            {
                options.WriteIndented = true;
            }
            catch (InvalidOperationException ex)
            {
                output.WriteLine(ex.Message);
            }

            var indented = new XmlSerializerOptions(options) { WriteIndented = true, NewLine = "\n" };
            output.WriteLine($"The copy:         IsReadOnly = {indented.IsReadOnly}");
            output.WriteLine(XmlSerializer.Serialize(person, indented));
        }

        #endregion

    }

}
