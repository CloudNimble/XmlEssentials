using System;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 21: reads comments, white space, CDATA, processing instructions, and the XML declaration with
    /// <see cref="Utf8XmlReader"/>, skips elements, and handles limits and errors.
    /// </summary>
    public static class ReadMarkupAndHandleErrors
    {

        #region Public Methods

        /// <summary>
        /// Reads one document twice: with the default options, which report everything, and with comments and
        /// white space between elements left out.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var xml = "<?xml-stylesheet href=\"order.xsl\"?>\n<Order>\n  <!-- rush -->\n  <Note><![CDATA[a < b]]></Note>\n</Order>"u8;

            output.WriteLine("Default options:");
            PrintTokens(xml, new XmlReaderOptions(), output);

            var dataOnly = new XmlReaderOptions
            {
                CommentHandling = XmlCommentHandling.Skip,
                WhitespaceHandling = XmlWhitespaceHandling.Insignificant,
            };

            output.WriteLine("Comments skipped and white space insignificant:");
            PrintTokens(xml, dataOnly, output);
        }

        /// <summary>
        /// Reads the XML declaration and shows one rule that differs between XML 1.0 and XML 1.1.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadDeclaration(TextWriter output)
        {
            var reader = new Utf8XmlReader("<?xml version=\"1.1\" encoding=\"utf-8\" standalone=\"yes\"?><Note>one\u0085two</Note>"u8);
            output.WriteLine($"Before the first read: {reader.Version}");

            reader.Read();
            output.WriteLine($"{reader.TokenType}: {reader.Version}");

            reader.Read();
            reader.Read();
            output.WriteLine($"XML 1.1 text: {Escape(reader.GetString())}");

            reader = new Utf8XmlReader("<Note>one\u0085two</Note>"u8);
            reader.Read();
            reader.Read();
            output.WriteLine($"XML 1.0 text: {Escape(reader.GetString())} ({reader.Version})");
        }

        /// <summary>
        /// Reads the version, encoding, and standalone flag of a declaration from a parsed <see cref="XmlDocument"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadDeclarationInfo(TextWriter output)
        {
            using var document = XmlDocument.Parse("<?xml version=\"1.1\" encoding=\"UTF-8\" standalone=\"yes\"?><Order/>"u8);

            if (document.Declaration is XmlDeclarationInfo declaration)
            {
                output.WriteLine($"Version: {declaration.Version}");
                output.WriteLine($"Encoding: {declaration.Encoding}");
                output.WriteLine($"Standalone: {declaration.Standalone}");
            }

            using var bare = XmlDocument.Parse("<Order/>"u8);
            output.WriteLine($"A document without a declaration has one: {bare.Declaration.HasValue}");
        }

        /// <summary>
        /// Skips an element and everything inside it.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void SkipElements(TextWriter output)
        {
            ReadOnlySpan<byte> xml = """
                <Order>
                  <Customer>
                    <Name>Ada</Name>
                    <Address><City>London</City></Address>
                  </Customer>
                  <Total>129.90</Total>
                </Order>
                """u8;

            var reader = new Utf8XmlReader(xml);
            while (reader.Read())
            {
                if (reader.TokenType is not XmlTokenType.StartElement)
                {
                    continue;
                }

                if (reader.LocalNameEquals("Customer"u8))
                {
                    reader.Skip();
                    output.WriteLine($"Skipped Customer. Now on {reader.TokenType} {Encoding.UTF8.GetString(reader.LocalNameSpan)} at depth {reader.CurrentDepth}.");
                }
                else
                {
                    output.WriteLine($"Read {Encoding.UTF8.GetString(reader.LocalNameSpan)}");
                }
            }
        }

        /// <summary>
        /// Reads a fragment that has more than one top-level element.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadFragment(TextWriter output)
        {
            var options = new XmlReaderOptions { AllowMultipleRoots = true };
            var reader = new Utf8XmlReader("<Item>Keyboard</Item><Item>Mouse</Item>"u8, options);
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.Text)
                {
                    output.WriteLine(reader.GetString());
                }
            }
        }

        /// <summary>
        /// Sets limits on the reader and shows the exception that each one throws.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void EnforceLimits(TextWriter output)
        {
            var xml = "<Order note=\"gift\" rush=\"true\"><Customer><Name>Ada Lovelace</Name></Customer></Order>"u8;

            output.WriteLine(Drain(xml, new XmlReaderOptions { MaxDepth = 2 }));
            output.WriteLine(Drain(xml, new XmlReaderOptions { MaxAttributeCount = 1 }));
            output.WriteLine(Drain(xml, new XmlReaderOptions { MaxNameByteCount = 4 }));
            output.WriteLine(Drain(xml, new XmlReaderOptions { MaxTextByteCount = 8 }));
            output.WriteLine(Drain(xml, new XmlReaderOptions { MaxDocumentByteCount = 64 }));
            output.WriteLine(Drain(xml, new XmlReaderOptions()));
        }

        /// <summary>
        /// Catches the exception for a document that is not well formed and prints where the error is.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReportErrors(TextWriter output)
        {
            var xml = "<Order>\n  <Item>Keyboard</Itm>\n</Order>"u8;

            try
            {
                var reader = new Utf8XmlReader(xml);
                while (reader.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                output.WriteLine($"Message: {ex.Message}");
                output.WriteLine($"Line {ex.LineNumber}, position {ex.LinePosition} (byte {ex.BytePosition} of the document)");
                output.WriteLine($"Path: {ex.Path ?? "(not set)"}");
            }
        }

        /// <summary>
        /// Shows what the reader does with documents that use features it does not support.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RejectUnsupported(TextWriter output)
        {
            output.WriteLine(Drain("<!DOCTYPE Order [<!ENTITY shop \"Contoso\">]><Order>&shop;</Order>"u8, new XmlReaderOptions()));
            output.WriteLine(Drain("<?xml version=\"1.0\" encoding=\"iso-8859-1\"?><Order/>"u8, new XmlReaderOptions()));
            output.WriteLine(Drain("<Order>Fish&nbsp;chips</Order>"u8, new XmlReaderOptions()));
            output.WriteLine(Drain("<Order>Fish &amp; chips &#169; &#x2014;</Order>"u8, new XmlReaderOptions()));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads a document to its end.
        /// </summary>
        /// <param name="utf8Xml">The document.</param>
        /// <param name="options">The options to read it with.</param>
        /// <returns>The number of tokens read, or the message of the exception the reader threw.</returns>
        private static string Drain(ReadOnlySpan<byte> utf8Xml, XmlReaderOptions options)
        {
            try
            {
                var tokens = 0;
                var reader = new Utf8XmlReader(utf8Xml, options);
                while (reader.Read())
                {
                    tokens++;
                }

                return $"Read {tokens} tokens.";
            }
            catch (XmlException ex)
            {
                return $"XmlException: {ex.Message}";
            }
        }

        /// <summary>
        /// Makes line feeds and next-line characters visible.
        /// </summary>
        /// <param name="text">The text to escape.</param>
        /// <returns>The text with each line feed shown as <c>\n</c> and each next-line character as <c>\u0085</c>.</returns>
        private static string Escape(string? text)
        {
            return (text ?? string.Empty).Replace("\n", "\\n").Replace("\u0085", "\\u0085");
        }

        /// <summary>
        /// Prints the type of every token in a document, with its name or value.
        /// </summary>
        /// <param name="utf8Xml">The document.</param>
        /// <param name="options">The options to read it with.</param>
        /// <param name="output">The writer that receives the lines.</param>
        private static void PrintTokens(ReadOnlySpan<byte> utf8Xml, XmlReaderOptions options, TextWriter output)
        {
            var reader = new Utf8XmlReader(utf8Xml, options);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case XmlTokenType.StartElement:
                    case XmlTokenType.EndElement:
                        output.WriteLine($"  {reader.TokenType} {Encoding.UTF8.GetString(reader.LocalNameSpan)}");
                        break;

                    case XmlTokenType.Comment:
                        output.WriteLine($"  {reader.TokenType} \"{reader.GetComment()}\"");
                        break;

                    case XmlTokenType.ProcessingInstruction:
                        output.WriteLine($"  {reader.TokenType} target={Encoding.UTF8.GetString(reader.LocalNameSpan)} data=\"{reader.GetString()}\"");
                        break;

                    default:
                        output.WriteLine($"  {reader.TokenType} \"{Escape(reader.GetString())}\"");
                        break;
                }
            }
        }

        #endregion

    }

}
