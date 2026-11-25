using System.IO;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 5: changes a document in memory with the mutable <see cref="XmlNode"/> tree.
    /// </summary>
    public static class EditWithXmlNode
    {

        #region Public Methods

        /// <summary>
        /// Parses an order, sets an attribute, adds an item, and writes the result.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var order = (XmlElementNode)XmlNode.Parse("""<Order id="1042"><Item>Keyboard</Item></Order>""")!;

            order.SetAttribute("status", "shipped");
            order.AddElement("Item").Add(new XmlTextNode("Mouse"));

            output.WriteLine(order.ToString());
        }

        #endregion

    }

}
