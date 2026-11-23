using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 43: the <see cref="XmlDocument"/> and <see cref="XmlNode"/> code that replaces LINQ to XML and <c>System.Xml.XmlDocument</c> code.
    /// </summary>
    public static class MigrateFromXDocument
    {

        #region Fields

        /// <summary>
        /// Specifies the document every method in this sample reads.
        /// </summary>
        private const string LibraryXml = """
            <Library>
              <Shelf name="Fiction">
                <Book id="1" year="1965"><Title>Dune</Title></Book>
                <Book id="2" year="1984"><Title>Neuromancer</Title></Book>
              </Shelf>
              <Shelf name="Science">
                <Book id="3" year="1988"><Title>A Brief History of Time</Title></Book>
              </Shelf>
            </Library>
            """;

        #endregion

        #region Public Methods

        /// <summary>
        /// Parses a document that contains a comment and indentation, writes it back, and then parses it without either.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void CommentsAndWhitespace(TextWriter output)
        {
            const string xml = """
                <Order>
                  <!-- rush -->
                  <Item>Keyboard</Item>
                </Order>
                """;

            using (XmlDocument document = XmlDocument.Parse(xml))
            {
                var buffer = new ArrayBufferWriter<byte>();
                using (var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true }))
                {
                    document.WriteTo(writer);
                }

                output.WriteLine($"Default: {CountChildren(document.RootElement)} children");
                output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
            }

            var elementsOnly = new XmlDocumentOptions
            {
                CommentHandling = XmlCommentHandling.Skip,
                WhitespaceHandling = XmlWhitespaceHandling.Insignificant,
            };

            using (XmlDocument document = XmlDocument.Parse(xml, elementsOnly))
            {
                output.WriteLine($"Skip and Insignificant: {CountChildren(document.RootElement)} child");
            }
        }

        /// <summary>
        /// Finds every <c>Title</c> element at any depth, which LINQ to XML does with <c>Descendants</c>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Descendants(TextWriter output)
        {
            using XmlDocument document = XmlDocument.Parse(LibraryXml);

            var titles = new List<string>();
            CollectTitles(document.RootElement, titles);

            output.WriteLine(string.Join(", ", titles));
        }

        /// <summary>
        /// Changes the document with the mutable <see cref="XmlNode"/> tree: sets an attribute, adds an element, and removes one.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Edit(TextWriter output)
        {
            var library = (XmlElementNode)XmlNode.Parse(
                """<Library><Shelf name="Fiction"><Book id="1" year="1965"><Title>Dune</Title></Book><Book id="2" year="1984"><Title>Neuromancer</Title></Book></Shelf></Library>""")!;

            XmlElementNode fiction = library["Shelf"]!;
            fiction.SetAttribute("name", "Classics");

            XmlElementNode book = fiction.AddElement("Book");
            book.SetAttribute("id", "4");
            book.SetAttribute("year", "1951");
            book.AddElement("Title").Add(new XmlTextNode("Foundation"));

            XmlElementNode neuromancer = fiction.Children
                .OfType<XmlElementNode>()
                .First(b => b.Attributes["id"]?.Value == "2");
            fiction.Children.Remove(neuromancer);

            output.WriteLine(fiction.ToString());
        }

        /// <summary>
        /// Reads the document with the read-only <see cref="XmlDocument"/>: the root, child elements, attributes, and typed values.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            using XmlDocument document = XmlDocument.Parse(LibraryXml);
            XmlElement library = document.RootElement;

            foreach (XmlElement shelf in library.EnumerateChildren())
            {
                if (shelf.ValueKind is not XmlValueKind.Element || shelf.LocalName != "Shelf")
                {
                    continue;
                }

                output.WriteLine($"Shelf: {shelf.GetAttribute("name").GetString()}");

                foreach (XmlElement book in shelf.EnumerateChildren())
                {
                    if (book.ValueKind is not XmlValueKind.Element || book.LocalName != "Book")
                    {
                        continue;
                    }

                    string? title = book.GetChild("Title").GetString();
                    int year = book.GetAttribute("year").GetInt32();
                    output.WriteLine($"  {title} ({year})");
                }
            }

            if (!library.TryGetChild("Annex", out _))
            {
                output.WriteLine("No annex");
            }
        }

        /// <summary>
        /// Writes a tree to a stream, which LINQ to XML does with <c>Save</c>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Save(TextWriter output)
        {
            var order = new XmlElementNode("Order");
            order.SetAttribute("id", "1042");
            order.AddElement("Item").Add(new XmlTextNode("Keyboard"));
            order.AddElement("Item").Add(new XmlTextNode("Mouse"));

            using var stream = new MemoryStream();
            var options = new XmlWriterOptions { Indented = true, NewLine = "\n" };

            using (var writer = new Utf8XmlWriter(stream, options))
            {
                order.WriteTo(writer);
            }

            output.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds the text of every <c>Title</c> element under <paramref name="element"/> to <paramref name="titles"/>.
        /// </summary>
        /// <param name="element">The element to search.</param>
        /// <param name="titles">The list that receives the titles.</param>
        private static void CollectTitles(XmlElement element, List<string> titles)
        {
            foreach (XmlElement child in element.EnumerateChildren())
            {
                if (child.ValueKind is not (XmlValueKind.Element or XmlValueKind.EmptyElement))
                {
                    continue;
                }

                if (child.LocalName == "Title")
                {
                    titles.Add(child.GetString()!);
                }

                CollectTitles(child, titles);
            }
        }

        /// <summary>
        /// Counts the child nodes of <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element whose children are counted.</param>
        /// <returns>The number of child nodes.</returns>
        private static int CountChildren(XmlElement element)
        {
            int count = 0;
            foreach (XmlElement _ in element.EnumerateChildren())
            {
                count++;
            }

            return count;
        }

        #endregion

    }

}
