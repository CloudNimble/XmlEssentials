using System;
using System.Globalization;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 20: reads tokens, names, attributes, values, and namespaces with <see cref="Utf8XmlReader"/>.
    /// </summary>
    public static class ReadTokensAndValues
    {

        #region Public Methods

        /// <summary>
        /// Reads every token of a document and prints its type, depth, and name or value.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            ReadOnlySpan<byte> xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <Order id="1042">
                  <Item>Keyboard</Item>
                  <Gift/>
                  <Note><![CDATA[Leave at <door>]]></Note>
                </Order>
                """u8;

            // Leave out comments and the white space between elements, so that the loop sees data only.
            var options = new XmlReaderOptions
            {
                CommentHandling = XmlCommentHandling.Skip,
                WhitespaceHandling = XmlWhitespaceHandling.Insignificant,
            };

            var reader = new Utf8XmlReader(xml, options);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case XmlTokenType.StartElement:
                    case XmlTokenType.EndElement:
                        output.WriteLine($"{reader.TokenType} {Encoding.UTF8.GetString(reader.LocalNameSpan)} (depth {reader.CurrentDepth})");
                        break;

                    case XmlTokenType.Text:
                    case XmlTokenType.CData:
                        output.WriteLine($"{reader.TokenType} \"{reader.GetString()}\" (depth {reader.CurrentDepth})");
                        break;

                    default:
                        output.WriteLine($"{reader.TokenType} (depth {reader.CurrentDepth})");
                        break;
                }
            }
        }

        /// <summary>
        /// Finds elements by name and walks the attributes of each start tag.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadAttributes(TextWriter output)
        {
            ReadOnlySpan<byte> xml = """
                <Order id="1042" currency="EUR">
                  <Item sku="KB-01" quantity="2">Keyboard</Item>
                  <Item sku="MS-07">Mouse</Item>
                </Order>
                """u8;

            var reader = new Utf8XmlReader(xml);
            while (reader.Read())
            {
                if (reader.TokenType is not XmlTokenType.StartElement || !reader.LocalNameEquals("Item"u8))
                {
                    continue;
                }

                output.WriteLine($"Item with {reader.AttributeCount} attribute(s)");
                while (reader.MoveToNextAttribute())
                {
                    output.WriteLine($"  {reader.TokenType} {Encoding.UTF8.GetString(reader.LocalNameSpan)} = {reader.GetString()}");
                }

                reader.MoveToElement();
                output.WriteLine($"  Back on {reader.TokenType} {Encoding.UTF8.GetString(reader.LocalNameSpan)}");
            }
        }

        /// <summary>
        /// Shows that an empty element is one token and that an element with a start tag and an end tag is two.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadEmptyElements(TextWriter output)
        {
            var reader = new Utf8XmlReader("<Order><Gift/><Note></Note></Order>"u8);
            var open = 0;
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.StartElement)
                {
                    if (!reader.IsEmptyElement)
                    {
                        open++;
                    }

                    output.WriteLine($"StartElement {Encoding.UTF8.GetString(reader.LocalNameSpan)}: IsEmptyElement = {reader.IsEmptyElement}, open elements = {open}");
                }
                else if (reader.TokenType is XmlTokenType.EndElement)
                {
                    open--;
                    output.WriteLine($"EndElement {Encoding.UTF8.GetString(reader.LocalNameSpan)}: open elements = {open}");
                }
            }
        }

        /// <summary>
        /// Reads attribute and element values as .NET types with the <c>Get*</c> and <c>TryGet*</c> methods.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadTypedValues(TextWriter output)
        {
            ReadOnlySpan<byte> xml = """
                <Order id="1042" rush="true">
                  <Placed>2026-03-14T09:30:00+01:00</Placed>
                  <Total>129.90</Total>
                  <Quantity>three</Quantity>
                </Order>
                """u8;

            var reader = new Utf8XmlReader(xml);
            while (reader.Read())
            {
                if (reader.TokenType is not XmlTokenType.StartElement)
                {
                    continue;
                }

                if (reader.LocalNameEquals("Order"u8))
                {
                    while (reader.MoveToNextAttribute())
                    {
                        if (reader.LocalNameEquals("id"u8))
                        {
                            output.WriteLine($"id: {reader.GetInt32()}");
                        }
                        else if (reader.LocalNameEquals("rush"u8))
                        {
                            output.WriteLine($"rush: {reader.GetBoolean()}");
                        }
                    }
                }
                else if (reader.LocalNameEquals("Placed"u8))
                {
                    reader.Read();
                    output.WriteLine($"Placed: {reader.GetDateTimeOffset():O}");
                }
                else if (reader.LocalNameEquals("Total"u8))
                {
                    reader.Read();
                    output.WriteLine($"Total: {reader.GetDecimal().ToString(CultureInfo.InvariantCulture)}");
                }
                else if (reader.LocalNameEquals("Quantity"u8))
                {
                    reader.Read();
                    if (reader.TryGetInt32(out var quantity))
                    {
                        output.WriteLine($"Quantity: {quantity}");
                    }
                    else
                    {
                        output.WriteLine($"Quantity is not a number: {reader.GetString()}");
                    }
                }
            }
        }

        /// <summary>
        /// Works with a value as UTF-8 bytes: compares it, measures it, and copies it without creating a string.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadValueBytes(TextWriter output)
        {
            var reader = new Utf8XmlReader("<Items><Item>Keyboard</Item><Item>Mouse &amp; pad</Item></Items>"u8);
            Span<char> characters = stackalloc char[32];
            while (reader.Read())
            {
                if (reader.TokenType is not XmlTokenType.Text)
                {
                    continue;
                }

                var written = reader.CopyString(characters);
                output.Write(characters[..written]);
                output.WriteLine($": {reader.ValueSpan.Length} bytes, escaped in the source = {reader.ValueIsEscaped}, is Keyboard = {reader.ValueTextEquals("Keyboard"u8)}");
            }
        }

        /// <summary>
        /// Reads the prefix, local name, and namespace URI of elements and attributes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadNamespaces(TextWriter output)
        {
            ReadOnlySpan<byte> xml = """
                <feed xmlns="http://www.w3.org/2005/Atom" xmlns:dc="http://purl.org/dc/elements/1.1/">
                  <title lang="en">Orders</title>
                  <dc:creator>Ada</dc:creator>
                </feed>
                """u8;

            var reader = new Utf8XmlReader(xml);
            while (reader.Read())
            {
                if (reader.TokenType is not XmlTokenType.StartElement)
                {
                    continue;
                }

                output.WriteLine($"Element prefix='{Encoding.UTF8.GetString(reader.PrefixSpan)}' local='{Encoding.UTF8.GetString(reader.LocalNameSpan)}' namespace='{Encoding.UTF8.GetString(reader.NamespaceUriSpan)}'");
                if (reader.LocalNameEquals("creator"u8) && reader.NamespaceUriEquals("http://purl.org/dc/elements/1.1/"u8))
                {
                    output.WriteLine("  This is the Dublin Core creator element.");
                }

                while (reader.MoveToNextAttribute())
                {
                    output.WriteLine($"  Attribute prefix='{Encoding.UTF8.GetString(reader.PrefixSpan)}' local='{Encoding.UTF8.GetString(reader.LocalNameSpan)}' namespace='{Encoding.UTF8.GetString(reader.NamespaceUriSpan)}'");
                }
            }
        }

        #endregion

    }

}
