using System;
using System.Collections.Generic;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Shapes;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 14: puts serialized elements and attributes in XML namespaces.
    /// </summary>
    public static class Namespaces
    {

        #region Public Methods

        /// <summary>
        /// Puts a whole document in one namespace with <see cref="XmlSerializerOptions.DefaultNamespace"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var options = new XmlSerializerOptions
            {
                DefaultNamespace = "urn:example:shipping",
                WriteIndented = true,
                NewLine = "\n",
            };

            var shipment = new Shipment { Id = 1042, Carrier = "Contoso Freight" };
            string xml = XmlSerializer.Serialize(shipment, options);
            output.WriteLine(xml);

            Shipment? copy = XmlSerializer.Deserialize<Shipment>(xml, options);
            output.WriteLine($"Shipment {copy!.Id} travels with {copy.Carrier}");
        }

        /// <summary>
        /// Chooses the prefixes of a document with <see cref="XmlSerializerOptions.Namespaces"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ChoosePrefixes(TextWriter output)
        {
            var options = new XmlSerializerOptions
            {
                Namespaces = new Dictionary<string, string>
                {
                    [""] = "urn:example:billing",
                    ["audit"] = "urn:example:audit",
                    ["ship"] = "urn:example:shipping",
                },
                WriteIndented = true,
                NewLine = "\n",
            };

            var invoice = new Invoice
            {
                Number = 7,
                Revision = 2,
                Customer = "Fabrikam",
                Note = "Leave at the front desk",
            };

            output.WriteLine(XmlSerializer.Serialize(invoice, options));
        }

        /// <summary>
        /// Compares qualified names and reads the well-known namespace of an attribute.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void CompareNames(TextWriter output)
        {
            var written = new XmlName("Invoice", "urn:example:billing", "b");
            var expected = new XmlName("Invoice", "urn:example:billing");
            var unqualified = new XmlName("Invoice");

            output.WriteLine(written);
            output.WriteLine(expected);
            output.WriteLine(unqualified);
            output.WriteLine(written == expected);
            output.WriteLine(written == unqualified);

            var reader = new Utf8XmlReader("<Total xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\" />"u8);
            reader.Read();
            while (reader.MoveToNextAttribute())
            {
                if (reader.LocalNameEquals(XmlAttributes.Nil) && reader.NamespaceUriEquals(XmlNamespaces.Xsi))
                {
                    output.WriteLine($"xsi:nil is {reader.GetString()}");
                }
            }
        }

        /// <summary>
        /// Serializes a type whose root, one element, and one attribute each name a namespace.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void QualifyMembers(TextWriter output)
        {
            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };
            var invoice = new Invoice
            {
                Number = 7,
                Revision = 2,
                Customer = "Fabrikam",
                Note = "Leave at the front desk",
            };

            output.WriteLine(XmlSerializer.Serialize(invoice, options));
        }

        /// <summary>
        /// Serializes a type in a namespace with one element that is in no namespace.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void PutMemberInNoNamespace(TextWriter output)
        {
            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };
            var order = new LegacyOrder { Customer = "Fabrikam", Id = 7 };

            output.WriteLine(XmlSerializer.Serialize(order, options));
        }

        /// <summary>
        /// Deserializes a document that binds the same namespaces to prefixes of its own.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadPrefixes(TextWriter output)
        {
            string xml = """
                <bill:Invoice xmlns:bill="urn:example:billing" xmlns:a="urn:example:audit" xmlns:ship="urn:example:shipping" number="7" a:revision="2">
                  <bill:Customer>Fabrikam</bill:Customer>
                  <ship:Note>Leave at the front desk</ship:Note>
                </bill:Invoice>
                """;

            Invoice? invoice = XmlSerializer.Deserialize<Invoice>(xml);
            output.WriteLine($"Invoice {invoice!.Number}, revision {invoice.Revision}");
            output.WriteLine($"{invoice.Customer}: {invoice.Note}");
        }

        /// <summary>
        /// Declares a prefix on the type with <c>[XmlNamespaceDeclaration]</c>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void DeclareOnType(TextWriter output)
        {
            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n", OmitXmlDeclaration = true };
            var show = new PodcastShow { Title = "Field Notes", Author = "Contoso Radio" };

            output.WriteLine(XmlSerializer.Serialize(show, options));
        }

        /// <summary>
        /// Takes the prefixes of an existing document with <see cref="XmlDocument.DiscoverNamespaces(string, XmlDocumentOptions)"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void DiscoverPrefixes(TextWriter output)
        {
            string existing = """
                <Invoice xmlns="urn:example:billing" xmlns:a="urn:example:audit" number="6" a:revision="1">
                  <Customer>Fabrikam</Customer>
                  <s:Note xmlns:s="urn:example:shipping">Ring the bell</s:Note>
                </Invoice>
                """;

            var namespaces = XmlDocument.DiscoverNamespaces(existing);
            foreach (var (prefix, namespaceUri) in namespaces)
            {
                output.WriteLine($"'{prefix}' is {namespaceUri}");
            }

            var options = new XmlSerializerOptions { Namespaces = namespaces, WriteIndented = true, NewLine = "\n", OmitXmlDeclaration = true };
            var invoice = new Invoice { Number = 7, Revision = 2, Customer = "Fabrikam", Note = "Leave at the front desk" };
            output.WriteLine(XmlSerializer.Serialize(invoice, options));
        }

        /// <summary>
        /// Shows what happens when a document's root element is in a different namespace than the type expects.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RejectWrongNamespace(TextWriter output)
        {
            string xml = """<Invoice xmlns="urn:example:orders" number="7"><Customer>Fabrikam</Customer></Invoice>""";

            try
            {
                XmlSerializer.Deserialize<Invoice>(xml);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Declares the customary prefixes of a format with <see cref="KnownNamespaces"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void UseKnownNamespaces(TextWriter output)
        {
            var options = new XmlSerializerOptions { Namespaces = KnownNamespaces.Podcast, WriteIndented = true, NewLine = "\n", OmitXmlDeclaration = true };
            var show = new PodcastShow { Title = "Field Notes", Author = "Contoso Radio" };

            output.WriteLine(XmlSerializer.Serialize(show, options));
        }

        #endregion

    }

}
