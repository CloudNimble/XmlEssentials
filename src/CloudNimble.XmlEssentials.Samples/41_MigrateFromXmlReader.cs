using System;
using System.Globalization;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 41: the <see cref="Utf8XmlReader"/> code that replaces <c>System.Xml.XmlReader</c> code.
    /// </summary>
    public static class MigrateFromXmlReader
    {

        #region Public Methods

        /// <summary>
        /// Reads a document that has a DTD, which the reader rejects.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void DtdIsRejected(TextWriter output)
        {
            ReadOnlySpan<byte> xml = """
                <!DOCTYPE note [<!ENTITY company "Contoso">]>
                <note>&company;</note>
                """u8;

            try
            {
                var reader = new Utf8XmlReader(xml);
                while (reader.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Reads a stream by copying it into a buffer and reading the buffer.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadFromStream(TextWriter output)
        {
            using Stream stream = new MemoryStream("""<Order id="1042"><Total>129.90</Total></Order>"""u8.ToArray());

            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);

            var reader = new Utf8XmlReader(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.StartElement && reader.LocalNameEquals("Total"u8))
                {
                    reader.Read();
                    output.WriteLine($"Total: {reader.GetDecimal().ToString(CultureInfo.InvariantCulture)}");
                }
            }
        }

        /// <summary>
        /// Reads an inventory document with namespaces, attributes, typed values, and a skipped subtree.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            ReadOnlySpan<byte> xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <inv:Inventory xmlns:inv="urn:contoso:inventory">
                  <inv:Item sku="KB-01" discontinued="false">
                    <inv:Name>Keyboard</inv:Name>
                    <inv:Price>49.90</inv:Price>
                    <inv:History><inv:Change>Price raised</inv:Change></inv:History>
                  </inv:Item>
                  <inv:Item sku="MS-02" discontinued="true">
                    <inv:Name>Mouse</inv:Name>
                    <inv:Price>19.50</inv:Price>
                  </inv:Item>
                </inv:Inventory>
                """u8;

            var reader = new Utf8XmlReader(xml);
            while (reader.Read())
            {
                if (reader.TokenType is not XmlTokenType.StartElement || !reader.NamespaceUriEquals("urn:contoso:inventory"u8))
                {
                    continue;
                }

                if (reader.LocalNameEquals("Item"u8))
                {
                    output.WriteLine($"Item at depth {reader.CurrentDepth} with {reader.AttributeCount} attributes");
                    while (reader.MoveToNextAttribute())
                    {
                        if (reader.LocalNameEquals("sku"u8))
                        {
                            output.WriteLine($"  Sku: {reader.GetString()}");
                        }
                        else if (reader.LocalNameEquals("discontinued"u8))
                        {
                            output.WriteLine($"  Discontinued: {reader.GetBoolean()}");
                        }
                    }

                    reader.MoveToElement();
                }
                else if (reader.LocalNameEquals("Name"u8))
                {
                    reader.Read();
                    output.WriteLine($"  Name: {reader.GetString()}");
                }
                else if (reader.LocalNameEquals("Price"u8))
                {
                    reader.Read();
                    if (reader.TryGetDecimal(out decimal price))
                    {
                        output.WriteLine($"  Price: {price.ToString(CultureInfo.InvariantCulture)}");
                    }
                }
                else if (reader.LocalNameEquals("History"u8))
                {
                    reader.Skip();
                }
            }
        }

        /// <summary>
        /// Prints every token of a small document with its depth, to compare with the nodes <c>System.Xml.XmlReader</c> reports.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Tokens(TextWriter output)
        {
            ReadOnlySpan<byte> xml = """<?xml version="1.0"?><Order> <!-- rush --><Item/><Note>Fragile &amp; heavy</Note></Order>"""u8;

            var reader = new Utf8XmlReader(xml);
            while (reader.Read())
            {
                string name = Encoding.UTF8.GetString(reader.LocalNameSpan);
                string detail = reader.TokenType switch
                {
                    XmlTokenType.StartElement => $": {name}, empty: {reader.IsEmptyElement}",
                    XmlTokenType.EndElement => $": {name}",
                    XmlTokenType.Text => $": '{reader.GetString()}'",
                    _ => "",
                };

                output.WriteLine($"{reader.TokenType} at depth {reader.CurrentDepth}{detail}");
            }
        }

        /// <summary>
        /// Leaves out comments and white space, which the default options report, by setting <see cref="XmlReaderOptions"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WithOptions(TextWriter output)
        {
            ReadOnlySpan<byte> xml = "<Order><!-- rush --> <Item>Keyboard</Item></Order>"u8;

            var options = new XmlReaderOptions
            {
                CommentHandling = XmlCommentHandling.Skip,
                MaxDepth = 16,
                WhitespaceHandling = XmlWhitespaceHandling.Insignificant,
            };

            var reader = new Utf8XmlReader(xml, options);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case XmlTokenType.Comment:
                        output.WriteLine($"Comment: '{reader.GetComment()}'");
                        break;
                    case XmlTokenType.Text:
                        output.WriteLine($"Text: '{reader.GetString()}'");
                        break;
                    default:
                        output.WriteLine(reader.TokenType);
                        break;
                }
            }
        }

        #endregion

    }

}
