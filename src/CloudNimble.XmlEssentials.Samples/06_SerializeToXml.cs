using System;
using System.Buffers;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Serialization;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 6: writes objects as XML with <see cref="XmlSerializer"/>, to each kind of destination.
    /// </summary>
    public static class SerializeToXml
    {

        #region Public Methods

        /// <summary>
        /// Serializes a <see cref="PurchaseOrder"/> to a string.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var order = new PurchaseOrder
            {
                Id = 1042,
                Customer = "Contoso",
                Placed = new DateOnly(2026, 3, 14),
                Total = 129.90m,
            };

            string xml = XmlSerializer.Serialize(order);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Serializes text that holds markup characters, which the serializer escapes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void EscapedText(TextWriter output)
        {
            var customer = new InvoiceCustomer { Name = "Fabrikam & Sons <West>", Email = "billing@fabrikam.example" };
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(customer, options);
            output.WriteLine(xml);

            InvoiceCustomer? copy = XmlSerializer.Deserialize<InvoiceCustomer>(xml, options);
            output.WriteLine(copy!.Name);
        }

        /// <summary>
        /// Serializes an <see cref="Invoice"/> that holds another object and a list, with indentation.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Indented(TextWriter output)
        {
            var invoice = new Invoice
            {
                Number = 7,
                BillTo = new InvoiceCustomer { Name = "Contoso", Email = "ap@contoso.example" },
                Lines =
                [
                    new InvoiceLine { Sku = "KB-101", Quantity = 2 },
                    new InvoiceLine { Sku = "MS-220", Quantity = 1 },
                ],
            };

            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };

            string xml = XmlSerializer.Serialize(invoice, options);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Serializes without the XML declaration, and with an XML 1.1 declaration.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Declaration(TextWriter output)
        {
            var customer = new InvoiceCustomer { Name = "Contoso", Email = "ap@contoso.example" };

            var noDeclaration = new XmlSerializerOptions { OmitXmlDeclaration = true };
            output.WriteLine(XmlSerializer.Serialize(customer, noDeclaration));

            var version11 = new XmlSerializerOptions { XmlVersion = XmlVersion.Version11 };
            output.WriteLine(XmlSerializer.Serialize(customer, version11));
        }

        /// <summary>
        /// Serializes to a UTF-8 byte array.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ToUtf8Bytes(TextWriter output)
        {
            var customer = new InvoiceCustomer { Name = "Contoso", Email = "ap@contoso.example" };

            byte[] utf8 = XmlSerializer.SerializeToUtf8Bytes(customer);

            output.WriteLine($"{utf8.Length} bytes");
            output.WriteLine(Encoding.UTF8.GetString(utf8));
        }

        /// <summary>
        /// Serializes to an <see cref="IBufferWriter{T}"/> of bytes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ToBufferWriter(TextWriter output)
        {
            var customer = new InvoiceCustomer { Name = "Contoso", Email = "ap@contoso.example" };
            var buffer = new ArrayBufferWriter<byte>();

            XmlSerializer.Serialize(buffer, customer);

            output.WriteLine($"{buffer.WrittenCount} bytes");
            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Serializes to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ToStream(TextWriter output)
        {
            var customer = new InvoiceCustomer { Name = "Contoso", Email = "ap@contoso.example" };
            using var stream = new MemoryStream();

            XmlSerializer.Serialize(stream, customer);

            output.WriteLine($"{stream.Length} bytes");
            output.WriteLine(Encoding.UTF8.GetString(stream.ToArray()));
        }

        /// <summary>
        /// Serializes two objects into a document that a <see cref="Utf8XmlWriter"/> is writing.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ToWriter(TextWriter output)
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writerOptions = new XmlWriterOptions { Indented = true, NewLine = "\n" };

            using (var writer = new Utf8XmlWriter(buffer, writerOptions))
            {
                writer.WriteStartElement("Customers");
                XmlSerializer.Serialize(writer, new InvoiceCustomer { Name = "Contoso", Email = "ap@contoso.example" });
                XmlSerializer.Serialize(writer, new InvoiceCustomer { Name = "Fabrikam", Email = "billing@fabrikam.example" });
                writer.WriteEndElement();
            }

            output.WriteLine(Encoding.UTF8.GetString(buffer.WrittenSpan));
        }

        /// <summary>
        /// Serializes a value whose type is known only at run time.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RuntimeType(TextWriter output)
        {
            object value = new InvoiceLine { Sku = "KB-101", Quantity = 2 };
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(value, value.GetType(), options);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Serializes to an <see cref="XmlDocument"/> and reads a value from it.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ToDocument(TextWriter output)
        {
            var customer = new InvoiceCustomer { Name = "Contoso", Email = "ap@contoso.example" };

            using XmlDocument document = XmlSerializer.SerializeToDocument(customer);

            XmlElement root = document.RootElement;
            output.WriteLine(root.LocalName);
            output.WriteLine(root.GetChild("Email").GetString());
        }

        #endregion

    }

}
