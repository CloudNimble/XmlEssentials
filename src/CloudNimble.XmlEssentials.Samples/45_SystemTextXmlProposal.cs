using System;
using System.Buffers;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Migration;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 45: the usage examples of the <c>System.Text.Xml</c> proposal (dotnet/runtime issue 125299), written with XmlEssentials.
    /// </summary>
    public static class SystemTextXmlProposal
    {

        #region Public Methods

        /// <summary>
        /// Serializes a type whose members are in two namespaces.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void NamespaceMapping(TextWriter output)
        {
            var person = new Employee
            {
                Name = "Alice",
                Age = 30,
                HomeAddress = new Address { City = "Seattle", Country = "US" },
            };

            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n", OmitXmlDeclaration = true };
            output.WriteLine(XmlSerializer.Serialize(person, options));
        }

        /// <summary>
        /// Serializes a list of a base type whose items are written under the names of their derived types.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Polymorphism(TextWriter output)
        {
            var catalog = new Catalog
            {
                Products =
                [
                    new Book { Title = "Clean Code", Author = "Robert C. Martin" },
                    new Dvd { Title = "The Matrix", Runtime = 136 },
                ],
            };

            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n", OmitXmlDeclaration = true };
            string xml = XmlSerializer.Serialize(catalog, options);
            output.WriteLine(xml);

            Catalog? copy = XmlSerializer.Deserialize<Catalog>(xml, options);
            output.WriteLine($"{copy!.Products[0].GetType().Name}, {copy.Products[1].GetType().Name}");
        }

        /// <summary>
        /// Reads a document in pieces, carrying the reader's state from one piece to the next.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadInChunks(TextWriter output)
        {
            byte[] xml = """<Order id="1042"><Item>Keyboard</Item><Item>Mouse</Item></Order>"""u8.ToArray();

            var state = new XmlReaderState(new XmlReaderOptions { MaxDepth = 64 });
            int start = 0;
            int end = 0;

            while (end < xml.Length)
            {
                end = Math.Min(end + 24, xml.Length);
                bool isFinal = end == xml.Length;

                var reader = new Utf8XmlReader(xml.AsSpan(start, end - start), isFinalBlock: isFinal, state);
                while (reader.Read())
                {
                    output.WriteLine($"Token: {reader.TokenType}, Depth: {reader.CurrentDepth}, Consumed: {reader.BytesConsumed}");
                }

                start += (int)reader.BytesConsumed;
                state = reader.CurrentState;
            }
        }

        /// <summary>
        /// Deserializes from a reader over a <see cref="ReadOnlySequence{T}"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadSequence(TextWriter output)
        {
            var utf8XmlSequence = new ReadOnlySequence<byte>("<Person><Name>Carol</Name><Age>41</Age></Person>"u8.ToArray());

            var reader = new Utf8XmlReader(utf8XmlSequence);
            Contact? person = XmlSerializer.Deserialize<Contact>(ref reader);

            output.WriteLine($"{person!.Name} is {person.Age}");
        }

        /// <summary>
        /// Reads a document and compares names by local name and namespace URI.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReaderNamespaces(TextWriter output)
        {
            ReadOnlySpan<byte> utf8Xml = """
                <root xmlns:ns="http://example.com">
                  <ns:Child>value</ns:Child>
                </root>
                """u8;

            var reader = new Utf8XmlReader(utf8Xml);
            while (reader.Read())
            {
                if (reader.TokenType == XmlTokenType.StartElement)
                {
                    if (reader.LocalNameEquals("Child"u8) &&
                        reader.NamespaceUriEquals("http://example.com"u8))
                    {
                        reader.Read();
                        string? value = reader.GetString();
                        output.WriteLine(value);
                    }
                }
            }
        }

        /// <summary>
        /// Serializes and deserializes with a string, a UTF-8 span, and a character span.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var person = new Contact { Name = "Alice", Age = 30 };

            string xml = XmlSerializer.Serialize(person);
            Contact? p = XmlSerializer.Deserialize<Contact>(xml);
            output.WriteLine(xml);
            output.WriteLine($"{p!.Name} is {p.Age}");

            ReadOnlySpan<byte> utf8XmlSpan = "<Person><Name>Bob</Name></Person>"u8;
            Contact? p2 = XmlSerializer.Deserialize<Contact>(utf8XmlSpan);
            output.WriteLine($"{p2!.Name} is {p2.Age}");

            ReadOnlySpan<char> xmlSpan = "<Person><Name>Alice</Name><Age>30</Age></Person>".AsSpan();
            Contact? p3 = XmlSerializer.Deserialize<Contact>(xmlSpan);
            output.WriteLine($"{p3!.Name} is {p3.Age}");
        }

        /// <summary>
        /// Serializes and deserializes with source-generated metadata.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void SourceGeneration(TextWriter output)
        {
            var line = new OrderLine { Sku = "KB-01", Description = "Keyboard" };

            string xml = XmlSerializer.Serialize(line, MigrationXmlContext.Default.OrderLine);
            OrderLine? copy = XmlSerializer.Deserialize(xml, MigrationXmlContext.Default.OrderLine);
            output.WriteLine(xml);
            output.WriteLine($"{copy!.Sku}: {copy.Description}");

            ReadOnlySpan<byte> utf8 = Encoding.UTF8.GetBytes(xml);
            OrderLine? copy2 = XmlSerializer.Deserialize(utf8, MigrationXmlContext.Default.OrderLine);
            output.WriteLine($"{copy2!.Sku}: {copy2.Description}");
        }

        /// <summary>
        /// Writes a document with a declared prefix, a default namespace, and a prefixed attribute.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriterNamespaces(TextWriter output)
        {
            using var stream = new MemoryStream();

            using (var writer = new Utf8XmlWriter(stream, new XmlWriterOptions { Indented = true, NewLine = "\n" }))
            {
                writer.WriteXmlDeclaration();
                writer.WriteStartElement("root");
                writer.WriteNamespaceDeclaration("ns", "http://example.com");
                writer.WriteDefaultNamespace("http://example.com/default");

                writer.WriteStartElement("ns", "Child", "http://example.com");
                writer.WriteAttributeString("ns", "id", "http://example.com", "42");
                writer.WriteStringValue("Hello");
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.Flush();
            }

            output.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
        }

        #endregion

    }

}
