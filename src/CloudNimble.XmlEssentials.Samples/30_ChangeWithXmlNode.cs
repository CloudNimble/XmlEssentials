using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.XmlEssentials.Samples.Models.Dom;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 30: parses XML into the mutable <see cref="XmlNode"/> tree, then reads and changes it.
    /// </summary>
    public static class ChangeWithXmlNode
    {

        #region Public Methods

        /// <summary>
        /// Parses a document into a tree and reads elements, attributes, and text from it.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            const string xml = """
                <Order id="1042" status="open">
                  <Customer>Ada Lovelace</Customer>
                  <Item sku="KB-1">Keyboard</Item>
                  <Item sku="MS-2">Mouse</Item>
                </Order>
                """;

            var order = (XmlElementNode)XmlNode.Parse(xml)!;

            output.WriteLine($"Customer: {order["Customer"]!.InnerText}");
            output.WriteLine($"Status: {order.Attributes["status"]!.Value}");
            output.WriteLine($"Discount element found: {order["Discount"] is not null}");
            output.WriteLine($"{order.Children.Count} child nodes, {order.OfType<XmlElementNode>().Count()} of them elements, {order.Attributes.Count} attributes");

            foreach (XmlNode child in order.Children)
            {
                if (child is XmlElementNode { LocalName: "Item" } item)
                {
                    output.WriteLine($"{item.Attributes["sku"]!.Value}: {item.InnerText}");
                }
            }

            XmlNode first = order.Children[0];
            output.WriteLine($"The first child is a {first.ValueKind} node, and its parent is <{((XmlElementNode)first.Parent!).LocalName}>");
        }

        /// <summary>
        /// Adds nodes at the end of an element and at a position inside it.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void AddAndInsert(TextWriter output)
        {
            var order = (XmlElementNode)XmlNode.Parse("""<Order><Item>Keyboard</Item></Order>""")!;

            order.AddElement("Item").InnerText = "Mouse";

            var customer = new XmlElementNode("Customer");
            customer.InnerText = "Ada Lovelace";
            order.Children.Insert(0, customer);

            order.Children.Insert(1, new XmlCommentNode(" items "));

            output.WriteLine(order.ToString());
        }

        /// <summary>
        /// Sets, changes, and removes attributes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Attributes(TextWriter output)
        {
            var order = (XmlElementNode)XmlNode.Parse("""<Order id="1042" status="open" draft="true"/>""")!;

            order.SetAttribute("status", "shipped");
            order.SetAttribute("carrier", "Contoso Freight");
            order.Attributes["id"]!.Value = "1043";

            XmlAttributeNode draft = order.Attributes["draft"]!;
            order.Attributes.Remove(draft);

            output.WriteLine(order.ToString());
        }

        /// <summary>
        /// Copies a subtree so that it can be added to a second parent.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void DeepClone(TextWriter output)
        {
            var order = (XmlElementNode)XmlNode.Parse("""<Order><Item sku="KB-1">Keyboard</Item></Order>""")!;
            XmlElementNode item = order["Item"]!;
            var backorder = new XmlElementNode("Backorder");

            try
            {
                backorder.Add(item);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }

            var copy = (XmlElementNode)item.DeepClone();
            copy.SetAttribute("sku", "KB-1B");
            backorder.Add(copy);

            output.WriteLine(order.ToString());
            output.WriteLine(backorder.ToString());
        }

        /// <summary>
        /// Adds an element to an indented tree, and then builds the same tree without the white space.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Indentation(TextWriter output)
        {
            const string xml = """
                <Order>
                  <Item>Keyboard</Item>
                </Order>
                """;

            var indented = (XmlElementNode)XmlNode.Parse(xml)!;
            indented.AddElement("Item").InnerText = "Mouse";
            output.WriteLine(indented.ToString());

            var options = new XmlDocumentOptions { WhitespaceHandling = XmlWhitespaceHandling.Insignificant };
            using XmlDocument document = XmlDocument.Parse(xml, options);
            var compact = (XmlElementNode)document.RootElement.CloneNode();
            compact.AddElement("Item").InnerText = "Mouse";
            output.WriteLine(compact.ToString());

            var buffer = new ArrayBufferWriter<byte>();
            var writerOptions = new XmlWriterOptions { Indented = true, NewLine = "\n", OmitXmlDeclaration = true };
            using (var writer = new Utf8XmlWriter(buffer, writerOptions))
            {
                compact.WriteTo(writer);
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Parses a document that has a declaration, comments, white space, and an empty element pair, and prints what the tree kept.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ParseKeeps(TextWriter output)
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <!-- exported nightly -->
                <Order>
                  <!-- rush -->
                  <Item> Keyboard </Item>
                  <Notes></Notes>
                </Order>
                """;

            XmlNode? order = XmlNode.Parse(xml);
            output.WriteLine(order!.ToString());

            try
            {
                XmlNode.Parse("<Order><Item>Keyboard</Order>");
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Reads a feed with a <see cref="Utf8XmlReader"/> and turns one entry into a tree.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ParseValue(TextWriter output)
        {
            ReadOnlySpan<byte> feed = """<Feed><Entry id="1"><Title>First post</Title></Entry><Entry id="2"><Title>Second post</Title></Entry></Feed>"""u8;

            var reader = new Utf8XmlReader(feed);
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.StartElement && reader.LocalNameEquals("Entry"u8))
                {
                    var entry = (XmlElementNode)XmlNode.ParseValue(ref reader)!;
                    entry.SetAttribute("read", "true");
                    output.WriteLine(entry.ToString());
                }
            }
        }

        /// <summary>
        /// Removes nodes, replaces a node, and moves a node to another parent.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RemoveAndReplace(TextWriter output)
        {
            var order = (XmlElementNode)XmlNode.Parse("""<Order><Item>Keyboard</Item><Item>Mouse</Item><Item>Monitor</Item><Note>Gift</Note></Order>""")!;

            order.Children.RemoveAt(0);

            XmlElementNode note = order["Note"]!;
            order.Children.Remove(note);
            output.WriteLine($"The removed node has a parent: {note.Parent is not null}");

            var replacement = new XmlElementNode("Item");
            replacement.InnerText = "Trackball";
            order.Children[0] = replacement;

            var archive = new XmlElementNode("Archive");
            archive.Add(note);

            order["Item"]!.InnerText = "Trackball (wireless)";

            output.WriteLine(order.ToString());
            output.WriteLine(archive.ToString());
        }

        /// <summary>
        /// Serializes an object to a tree, changes the tree, and deserializes it back.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Serializer(TextWriter output)
        {
            var product = new Product { Name = "Keyboard", Price = 49.90m };

            var node = (XmlElementNode)XmlSerializer.SerializeToNode(product)!;
            output.WriteLine(node.ToString());

            node["Price"]!.InnerText = "44.90";

            Product? discounted = XmlSerializer.Deserialize<Product>(node);
            output.WriteLine($"{discounted!.Name}: {discounted.Price.ToString(CultureInfo.InvariantCulture)}");
        }

        #endregion

    }

}
