using System.IO;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Advanced;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 34: the serializer calls that are safe in a trimmed or Native AOT application.
    /// </summary>
    public static class NativeAot
    {

        #region Public Methods

        /// <summary>
        /// Round-trips an order as UTF-8 with overloads that take type info, which need no reflection.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var order = new Order
            {
                Id = 1042,
                Customer = "Ada",
                Lines = [new OrderLine { Sku = "KB-01", Quantity = 1 }],
            };

            byte[] utf8 = XmlSerializer.SerializeToUtf8Bytes(order, OrderXmlContext.Default.Order);
            output.WriteLine(Encoding.UTF8.GetString(utf8));

            Order? copy = XmlSerializer.Deserialize(utf8, OrderXmlContext.Default.Order);
            output.WriteLine($"Order {copy!.Id} for {copy.Customer}: {copy.Lines.Count} line");
        }

        /// <summary>
        /// Writes an order to a stream with the overload that takes a context.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteToStream(TextWriter output)
        {
            var order = new Order { Id = 7, Customer = "Grace" };

            using var stream = new MemoryStream();
            XmlSerializer.Serialize(stream, order, OrderXmlContext.Default);
            output.WriteLine($"{stream.Length} bytes");

            stream.Position = 0;
            Order? copy = XmlSerializer.Deserialize<Order>(stream, OrderXmlContext.Default);
            output.WriteLine($"Order {copy!.Id} for {copy.Customer}");
        }

        #endregion

    }

}
