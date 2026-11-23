using System;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Samples.Models.Serialization;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 7: reads XML into objects with <see cref="XmlSerializer"/>, from each kind of source.
    /// </summary>
    public static class DeserializeFromXml
    {

        #region Public Methods

        /// <summary>
        /// Deserializes a <see cref="PurchaseOrder"/> from a string.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <PurchaseOrder>
                  <Id>1042</Id>
                  <Customer>Contoso</Customer>
                  <Placed>2026-03-14</Placed>
                  <Total>129.90</Total>
                </PurchaseOrder>
                """;

            PurchaseOrder? order = XmlSerializer.Deserialize<PurchaseOrder>(xml);

            output.WriteLine($"Order {order!.Id} for {order.Customer}");
            output.WriteLine($"Placed {order.Placed:yyyy-MM-dd}");
            output.WriteLine($"Total is 129.90: {order.Total == 129.90m}");
        }

        /// <summary>
        /// Deserializes from UTF-8 bytes without converting them to a string first.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void FromUtf8Bytes(TextWriter output)
        {
            ReadOnlySpan<byte> utf8 = "<InvoiceCustomer><Name>Contoso</Name><Email>ap@contoso.example</Email></InvoiceCustomer>"u8;

            InvoiceCustomer? customer = XmlSerializer.Deserialize<InvoiceCustomer>(utf8);

            output.WriteLine($"{customer!.Name} <{customer.Email}>");
        }

        /// <summary>
        /// Deserializes from a <see cref="Stream"/> of UTF-8 bytes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void FromStream(TextWriter output)
        {
            byte[] file = Encoding.UTF8.GetBytes("<InvoiceCustomer><Name>Contoso</Name><Email>ap@contoso.example</Email></InvoiceCustomer>");
            using var stream = new MemoryStream(file);

            InvoiceCustomer? customer = XmlSerializer.Deserialize<InvoiceCustomer>(stream);

            output.WriteLine($"{customer!.Name} <{customer.Email}>");
        }

        /// <summary>
        /// Deserializes each <c>InvoiceCustomer</c> element that a <see cref="Utf8XmlReader"/> finds in a larger document.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void FromReader(TextWriter output)
        {
            ReadOnlySpan<byte> utf8 = """
                <Customers exported="2026-03-14">
                  <InvoiceCustomer><Name>Contoso</Name><Email>ap@contoso.example</Email></InvoiceCustomer>
                  <InvoiceCustomer><Name>Fabrikam</Name><Email>billing@fabrikam.example</Email></InvoiceCustomer>
                </Customers>
                """u8;

            var reader = new Utf8XmlReader(utf8);
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.StartElement && reader.LocalNameEquals("InvoiceCustomer"u8))
                {
                    InvoiceCustomer? customer = XmlSerializer.Deserialize<InvoiceCustomer>(ref reader);
                    output.WriteLine($"{customer!.Name} <{customer.Email}>");
                }
            }
        }

        /// <summary>
        /// Deserializes one element of a parsed <see cref="XmlDocument"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void FromDocument(TextWriter output)
        {
            const string xml = """
                <Response status="ok">
                  <InvoiceCustomer><Name>Contoso</Name><Email>ap@contoso.example</Email></InvoiceCustomer>
                </Response>
                """;

            using XmlDocument document = XmlDocument.Parse(xml);
            XmlElement element = document.RootElement.GetChild("InvoiceCustomer");

            InvoiceCustomer? customer = XmlSerializer.Deserialize<InvoiceCustomer>(element);

            output.WriteLine($"{customer!.Name} <{customer.Email}>");
        }

        /// <summary>
        /// Deserializes into a type that is known only at run time.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RuntimeType(TextWriter output)
        {
            Type type = typeof(InvoiceLine);

            object? value = XmlSerializer.Deserialize("<InvoiceLine><Sku>KB-101</Sku><Quantity>2</Quantity></InvoiceLine>", type);

            var line = (InvoiceLine)value!;
            output.WriteLine($"{line.Quantity} x {line.Sku}");
        }

        /// <summary>
        /// Deserializes XML whose elements are out of order, with one element missing and one that no property matches.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void MissingAndUnknown(TextWriter output)
        {
            const string xml = """
                <PurchaseOrder priority="high">
                  <Customer>Contoso</Customer>
                  <Id>1042</Id>
                  <Warehouse>SEA-2</Warehouse>
                </PurchaseOrder>
                """;

            PurchaseOrder? order = XmlSerializer.Deserialize<PurchaseOrder>(xml);

            output.WriteLine($"Id: {order!.Id}");
            output.WriteLine($"Customer: {order.Customer}");
            output.WriteLine($"Placed: {order.Placed:yyyy-MM-dd}");
            output.WriteLine($"Total: {order.Total}");
        }

        /// <summary>
        /// Rejects XML that holds an element no property matches.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RejectUnknown(TextWriter output)
        {
            const string xml = "<PurchaseOrder><Id>1042</Id><Warehouse>SEA-2</Warehouse></PurchaseOrder>";
            var options = new XmlSerializerOptions { UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow };

            try
            {
                XmlSerializer.Deserialize<PurchaseOrder>(xml, options);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Shows that names are matched with case by default, and without case when the option is set.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void NameMatching(TextWriter output)
        {
            const string xml = "<PurchaseOrder><id>1042</id><CUSTOMER>Contoso</CUSTOMER></PurchaseOrder>";

            PurchaseOrder? exact = XmlSerializer.Deserialize<PurchaseOrder>(xml);
            output.WriteLine($"Exact: {exact!.Id}, {exact.Customer ?? "(null)"}");

            var options = new XmlSerializerOptions { PropertyNameCaseInsensitive = true };
            PurchaseOrder? relaxed = XmlSerializer.Deserialize<PurchaseOrder>(xml, options);
            output.WriteLine($"Case-insensitive: {relaxed!.Id}, {relaxed.Customer ?? "(null)"}");
        }

        /// <summary>
        /// Shows the exceptions thrown for a document element with the wrong name, XML that is not well formed, and a value that is not valid for its type.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void InvalidXml(TextWriter output)
        {
            string[] documents =
            [
                "<Order><Id>1042</Id></Order>",
                "<PurchaseOrder><Id>1042</PurchaseOrder>",
                "<PurchaseOrder><Id>ten</Id></PurchaseOrder>",
            ];

            foreach (string xml in documents)
            {
                try
                {
                    XmlSerializer.Deserialize<PurchaseOrder>(xml);
                }
                catch (XmlException ex)
                {
                    output.WriteLine(ex.Message);
                }
            }
        }

        #endregion

    }

}
