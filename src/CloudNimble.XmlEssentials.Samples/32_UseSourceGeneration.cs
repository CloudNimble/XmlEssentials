using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Advanced;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 32: the ways to call <see cref="XmlSerializer"/> with a source-generated context.
    /// </summary>
    public static class UseSourceGeneration
    {

        #region Public Methods

        /// <summary>
        /// Serializes and deserializes an order with the <c>XmlTypeInfo&lt;Order&gt;</c> property of the context.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            Order order = CreateOrder();

            string xml = XmlSerializer.Serialize(order, OrderXmlContext.Default.Order);
            output.WriteLine(xml);

            Order? copy = XmlSerializer.Deserialize(xml, OrderXmlContext.Default.Order);
            output.WriteLine($"Order {copy!.Id} for {copy.Customer}: {copy.Lines.Count} lines");
        }

        /// <summary>
        /// Uses a type info property that <c>TypeInfoPropertyName</c> renamed.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void NameTheTypeInfoProperty(TextWriter output)
        {
            var line = new OrderLine { Sku = "KB-01", Quantity = 1 };

            string xml = XmlSerializer.Serialize(line, OrderLineXmlContext.Default.Line);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Uses a context whose default options come from <c>[XmlSourceGenerationOptions]</c>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void SetOptionsAtCompileTime(TextWriter output)
        {
            string xml = XmlSerializer.Serialize(CreateOrder(), CamelCaseOrderXmlContext.Default.Order);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Creates a context from an <see cref="XmlSerializerOptions"/> instance.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void SetOptionsAtRunTime(TextWriter output)
        {
            var options = new XmlSerializerOptions
            {
                NewLine = "\n",
                OmitXmlDeclaration = true,
                WriteIndented = true,
            };
            var context = new OrderXmlContext(options);

            string xml = XmlSerializer.Serialize(CreateOrder(), context.Order);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Uses the type info of two contexts in the same application.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void UseSeveralContexts(TextWriter output)
        {
            var shipment = new Shipment { OrderId = 1042, Carrier = "Contoso Freight" };

            output.WriteLine(XmlSerializer.Serialize(CreateOrder(), OrderXmlContext.Default.Order));
            output.WriteLine(XmlSerializer.Serialize(shipment, ShippingXmlContext.Default.Shipment));
        }

        /// <summary>
        /// Passes the context itself and lets the serializer look up the type info.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void UseContextOverloads(TextWriter output)
        {
            string xml = XmlSerializer.Serialize(CreateOrder(), OrderXmlContext.Default);
            output.WriteLine(xml);

            Order? copy = XmlSerializer.Deserialize<Order>(xml, OrderXmlContext.Default);
            output.WriteLine($"Order {copy!.Id} for {copy.Customer}: {copy.Lines.Count} lines");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the order every method in this sample serializes.
        /// </summary>
        /// <returns>An order with two lines.</returns>
        private static Order CreateOrder()
        {
            return new Order
            {
                Id = 1042,
                Customer = "Ada",
                Lines =
                [
                    new OrderLine { Sku = "KB-01", Quantity = 1 },
                    new OrderLine { Sku = "MS-02", Quantity = 2 },
                ],
            };
        }

        #endregion

    }

}
