using System.IO;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 4: parses a document once and reads values from it with <see cref="XmlDocument"/>.
    /// </summary>
    public static class QueryWithXmlDocument
    {

        #region Public Methods

        /// <summary>
        /// Parses an order and reads its attribute, its items, and its total.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            const string xml = """
                <Order id="1042">
                  <Item>Keyboard</Item>
                  <Item>Mouse</Item>
                  <Total>129.90</Total>
                </Order>
                """;

            using XmlDocument document = XmlDocument.Parse(xml);
            XmlElement order = document.RootElement;

            output.WriteLine($"Order {order.GetAttribute("id").GetInt32()}");

            foreach (XmlElement child in order.EnumerateChildren())
            {
                if (child.ValueKind is XmlValueKind.Element && child.LocalName == "Item")
                {
                    output.WriteLine($"Item: {child.GetString()}");
                }
            }

            output.WriteLine($"Total: {order.GetChild("Total").GetDecimal()}");
        }

        #endregion

    }

}
