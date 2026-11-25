using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 26: parses XML into the read-only <see cref="XmlDocument"/> from each kind of input, and sets
    /// the options that control what the parser keeps and what it rejects.
    /// </summary>
    public static class ParseWithXmlDocument
    {

        #region Public Methods

        /// <summary>
        /// Parses an order from a stream without blocking the calling thread.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        /// <returns>A task that completes when the sample has run.</returns>
        public static async Task ParseFromStreamAsync(TextWriter output)
        {
            byte[] utf8 = """<Order id="1044"><Item>Monitor</Item></Order>"""u8.ToArray();

            await using var stream = new MemoryStream(utf8);
            using XmlDocument document = await XmlDocument.ParseAsync(stream);

            output.WriteLine($"order {document.RootElement.GetAttribute("id").GetInt32()}: {document.RootElement.GetChild("Item").GetString()}");
        }

        /// <summary>
        /// Parses the same order from a string, UTF-8 bytes, a stream, and a sequence.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            const string text = """<Order id="1042"><Item>Keyboard</Item></Order>""";
            byte[] utf8 = """<Order id="1043"><Item>Mouse</Item></Order>"""u8.ToArray();

            using (XmlDocument fromString = XmlDocument.Parse(text))
            {
                output.WriteLine($"string: order {fromString.RootElement.GetAttribute("id").GetInt32()}");
            }

            using (XmlDocument fromUtf8 = XmlDocument.Parse(utf8.AsSpan()))
            {
                output.WriteLine($"UTF-8 span: order {fromUtf8.RootElement.GetAttribute("id").GetInt32()}");
            }

            using (var stream = new MemoryStream(utf8))
            using (XmlDocument fromStream = XmlDocument.Parse(stream))
            {
                output.WriteLine($"stream: order {fromStream.RootElement.GetAttribute("id").GetInt32()}");
            }

            using (XmlDocument fromSequence = XmlDocument.Parse(new ReadOnlySequence<byte>(utf8)))
            {
                output.WriteLine($"sequence: order {fromSequence.RootElement.GetAttribute("id").GetInt32()}");
            }
        }

        /// <summary>
        /// Shows the three ways a document treats comments.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Comments(TextWriter output)
        {
            const string xml = "<Order><!-- rush --><Item>Keyboard</Item></Order>";

            using (XmlDocument kept = XmlDocument.Parse(xml))
            {
                output.WriteLine($"Allow (the default): {CountChildren(kept.RootElement)} children");
                foreach (XmlElement child in kept.RootElement.EnumerateChildren())
                {
                    output.WriteLine($"  {child.ValueKind}: '{child.GetString()}'");
                }
            }

            var skip = new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Skip };
            using (XmlDocument skipped = XmlDocument.Parse(xml, skip))
            {
                output.WriteLine($"Skip: {CountChildren(skipped.RootElement)} child");
            }

            var disallow = new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Disallow };
            try
            {
                using XmlDocument rejected = XmlDocument.Parse(xml, disallow);
            }
            catch (XmlException ex)
            {
                output.WriteLine($"Disallow: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the XML declaration of a document that has one and of a document that has none.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Declaration(TextWriter output)
        {
            using (XmlDocument document = XmlDocument.Parse("""<?xml version="1.1" encoding="UTF-8" standalone="yes"?><Order/>"""))
            {
                if (document.Declaration is { } declaration)
                {
                    output.WriteLine($"version {declaration.Version}, encoding {declaration.Encoding}, standalone {declaration.Standalone}");
                }
            }

            using (XmlDocument document = XmlDocument.Parse("<Order/>"))
            {
                output.WriteLine($"has a declaration: {document.Declaration.HasValue}");
            }
        }

        /// <summary>
        /// Copies what is needed out of a document before it is disposed, and shows what happens afterward.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Lifetime(TextWriter output)
        {
            XmlElement item;
            string? name;

            using (XmlDocument document = XmlDocument.Parse("<Order><Item>Keyboard</Item></Order>"))
            {
                item = document.RootElement.GetChild("Item");
                name = item.GetString();
            }

            output.WriteLine($"The copied string is still valid: {name}");

            try
            {
                item.GetString();
            }
            catch (ObjectDisposedException)
            {
                output.WriteLine("The element is not: ObjectDisposedException");
            }

            XmlElement empty = default;
            output.WriteLine($"A default element is {empty.ValueKind}");
        }

        /// <summary>
        /// Sets limits for input that is not trusted and shows what the parser reports when one is exceeded.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Limits(TextWriter output)
        {
            var options = new XmlDocumentOptions
            {
                MaxDepth = 2,
                MaxDocumentByteCount = 64,
            };

            try
            {
                using XmlDocument tooDeep = XmlDocument.Parse("<a><b><c/></b></a>", options);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }

            try
            {
                using XmlDocument tooLarge = XmlDocument.Parse($"<a>{new string('x', 100)}</a>", options);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Reads a feed with a <see cref="Utf8XmlReader"/> and builds one small document for each entry.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ParseValue(TextWriter output)
        {
            ReadOnlySpan<byte> feed = """
                <Feed>
                  <Entry id="1"><Title>First post</Title></Entry>
                  <Entry id="2"><Title>Second post</Title></Entry>
                </Feed>
                """u8;

            var reader = new Utf8XmlReader(feed);
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.StartElement && reader.LocalNameEquals("Entry"u8))
                {
                    using XmlDocument entry = XmlDocument.ParseValue(ref reader);
                    XmlElement root = entry.RootElement;
                    output.WriteLine($"{root.GetAttribute("id").GetInt32()}: {root.GetChild("Title").GetString()}");
                }
            }
        }

        /// <summary>
        /// Parses the elements that are all in a block of input, and stops at the one that is not.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void TryParseValue(TextWriter output)
        {
            ReadOnlySpan<byte> feed = """<Feed><Entry id="1"><Title>First post</Title></Entry><Entry id="2"><Title>Second post</Title></Entry></Feed>"""u8;

            // The first block of the input ends inside the second entry.
            var reader = new Utf8XmlReader(feed[..80], isFinalBlock: false, default);
            while (reader.Read())
            {
                if (reader.TokenType is not XmlTokenType.StartElement || !reader.LocalNameEquals("Entry"u8))
                {
                    continue;
                }

                if (!XmlDocument.TryParseValue(ref reader, out XmlDocument? entry))
                {
                    output.WriteLine($"The next entry isn't all here. The reader is still on its {reader.TokenType}.");
                    break;
                }

                using (entry)
                {
                    XmlElement root = entry.RootElement;
                    output.WriteLine($"{root.GetAttribute("id").GetInt32()}: {root.GetChild("Title").GetString()}");
                }
            }
        }

        /// <summary>
        /// Checks whether input is a well-formed document without catching an exception.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void TryParse(TextWriter output)
        {
            var options = new XmlDocumentOptions();

            if (XmlDocument.TryParse("<Order><Item>Keyboard</Item></Order>"u8, options, out XmlDocument? document))
            {
                using (document)
                {
                    output.WriteLine($"Parsed: {document!.RootElement.LocalName}");
                }
            }

            if (!XmlDocument.TryParse("<Order><Item>Keyboard</Order>"u8, options, out XmlDocument? broken))
            {
                output.WriteLine($"Not well formed: document is null: {broken is null}");
            }
        }

        /// <summary>
        /// Compares the children and the text of an indented document with white space kept and left out.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Whitespace(TextWriter output)
        {
            const string xml = """
                <Order>
                  <Item>Keyboard</Item>
                  <Item>Mouse</Item>
                </Order>
                """;

            using (XmlDocument document = XmlDocument.Parse(xml))
            {
                XmlElement order = document.RootElement;
                output.WriteLine($"Significant (the default): {CountChildren(order)} children");
                foreach (XmlElement child in order.EnumerateChildren())
                {
                    output.WriteLine($"  {child.ValueKind}");
                }

                output.WriteLine($"  Text of the root: '{order.GetString()!.Replace("\n", "\\n")}'");
                output.WriteLine($"  Text of the first Item: '{order.GetChild("Item").GetString()}'");
            }

            var options = new XmlDocumentOptions { WhitespaceHandling = XmlWhitespaceHandling.Insignificant };
            using (XmlDocument document = XmlDocument.Parse(xml, options))
            {
                XmlElement order = document.RootElement;
                output.WriteLine($"Insignificant: {CountChildren(order)} children");
                output.WriteLine($"  Text of the root: '{order.GetString()}'");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Counts the child nodes of an element.
        /// </summary>
        /// <param name="element">The element whose children are counted.</param>
        /// <returns>The number of child nodes of every kind.</returns>
        private static int CountChildren(XmlElement element)
        {
            var count = 0;
            foreach (XmlElement child in element.EnumerateChildren())
            {
                count++;
            }

            return count;
        }

        #endregion

    }

}
