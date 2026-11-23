using System.IO;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Advanced;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 31: the same serializer call made with reflection, with a source-generated context, and
    /// through an interceptor.
    /// </summary>
    public static class ReflectionVsSourceGeneration
    {

        #region Public Methods

        /// <summary>
        /// Serializes a type that no context lists, so the serializer maps it with reflection.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var receipt = new Receipt { Number = 77, Paid = 129.90m };
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(receipt, options);
            output.WriteLine(xml);

            Receipt? copy = XmlSerializer.Deserialize<Receipt>(xml, options);
            output.WriteLine($"Receipt {copy!.Number}: {copy.Paid}");
            output.WriteLine(options.TypeInfoResolver!.GetType().Name);
        }

        /// <summary>
        /// Shows that a call that passes no options and no type info is redirected to the generated context that lists the
        /// type, and that a call that passes options runs as it is written.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Interceptors(TextWriter output)
        {
            var shipment = new Shipment { OrderId = 1042, Carrier = "Contoso Freight" };

            string xml = XmlSerializer.Serialize(shipment);
            output.WriteLine(xml);

            Shipment? copy = XmlSerializer.Deserialize<Shipment>(Encoding.UTF8.GetBytes(xml));
            output.WriteLine($"Shipment {copy!.OrderId}: {copy.Carrier}");

            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            output.WriteLine(XmlSerializer.Serialize(shipment, options));
            output.WriteLine(options.TypeInfoResolver!.GetType().Name);
        }

        /// <summary>
        /// Serializes and deserializes an order with the type info of a source-generated context.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void SourceGeneration(TextWriter output)
        {
            var order = new Order
            {
                Id = 1042,
                Customer = "Ada",
                Lines =
                [
                    new OrderLine { Sku = "KB-01", Quantity = 1 },
                    new OrderLine { Sku = "MS-02", Quantity = 2 },
                ],
            };

            string xml = XmlSerializer.Serialize(order, OrderXmlContext.Default.Order);
            output.WriteLine(xml);

            Order? copy = XmlSerializer.Deserialize(xml, OrderXmlContext.Default.Order);
            output.WriteLine($"Order {copy!.Id} for {copy.Customer}: {copy.Lines.Count} lines");
        }

        #endregion

    }

}
