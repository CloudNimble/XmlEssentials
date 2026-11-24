using System;
using System.IO;
using CloudNimble.Text.Xml;
#if NET11_0_OR_GREATER
using CloudNimble.XmlEssentials.Samples.Models.Closed;
#endif
using CloudNimble.XmlEssentials.Samples.Models.Shapes;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 16: serializes the members of derived types.
    /// </summary>
    public static class Polymorphism
    {

        #region Public Methods

        /// <summary>
        /// Serializes a list of a base type, where each derived type is written under its own element name.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };
            var catalog = new Catalog
            {
                Products =
                [
                    new Book { Title = "Clean Code", Author = "Robert C. Martin" },
                    new Dvd { Title = "The Matrix", Runtime = 136 },
                    new Product { Title = "Gift card" },
                ],
            };

            string xml = XmlSerializer.Serialize(catalog, options);
            output.WriteLine(xml);

            Catalog? copy = XmlSerializer.Deserialize<Catalog>(xml, options);
            foreach (Product product in copy!.Products)
            {
                output.WriteLine($"{product.GetType().Name}: {product.Title}");
            }
        }

        /// <summary>
        /// Serializes a derived type as the document root, named by an <c>xsi:type</c> attribute, and a type the base
        /// does not declare as its nearest declared ancestor.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void FallBackToAncestor(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            Vehicle truck = new Truck { Make = "Northwind", Payload = 12000 };
            output.WriteLine(XmlSerializer.Serialize(truck, options));

            Vehicle sportsCar = new SportsCar { Make = "Contoso", Doors = 2, TopSpeed = 310 };
            string xml = XmlSerializer.Serialize(sportsCar, options);
            output.WriteLine(xml);

            Vehicle? copy = XmlSerializer.Deserialize<Vehicle>(xml, options);
            output.WriteLine(copy!.GetType().Name);
        }

        /// <summary>
        /// Serializes a non-public member that is marked with <c>[XmlInclude]</c>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void IncludeNonPublicMember(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            var account = new Account { Name = "Operations", LedgerCode = "OPS-17" };

            output.WriteLine(XmlSerializer.Serialize(account, options));
        }

        /// <summary>
        /// Shows that a runtime type the base type does not declare is rejected by default.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RejectUnknownType(TextWriter output)
        {
            var catalog = new Catalog
            {
                Products = [new Ebook { Title = "Refactoring", Author = "Martin Fowler", Format = "EPUB" }],
            };

            try
            {
                XmlSerializer.Serialize(catalog);
            }
            catch (NotSupportedException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Shows why the declared type matters: the same object serialized as its base type and as <see cref="object"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void SerializeDeclaredType(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            var card = new CardPayment { Amount = 59.90m, LastFour = "4242" };

            output.WriteLine(XmlSerializer.Serialize<Payment>(card, options));
            output.WriteLine(XmlSerializer.Serialize(card, options));
            output.WriteLine(XmlSerializer.Serialize(card, card.GetType(), options));
        }

#if NET11_0_OR_GREATER
        /// <summary>
        /// Takes the derived types of a closed type from the list the compiler makes, with no attributes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void InferClosedTypes(TextWriter output)
        {
            var options = new XmlSerializerOptions
            {
                InferClosedTypePolymorphism = true,
                NewLine = "\n",
                OmitXmlDeclaration = true,
                WriteIndented = true,
            };
            var outbox = new Outbox
            {
                Messages =
                {
                    new EmailNotification { Text = "Your order shipped.", Address = "ada@example.com" },
                    new SmsNotification { Text = "Your code is 4242.", Number = "+1 555 0100" },
                },
            };

            string xml = XmlSerializer.Serialize(outbox, options);
            output.WriteLine(xml);

            Outbox? copy = XmlSerializer.Deserialize<Outbox>(xml, options);
            foreach (Notification message in copy!.Messages)
            {
                output.WriteLine(message.GetType().Name);
            }
        }
#endif

        /// <summary>
        /// Names the derived type with an attribute on the member's element.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void UseAttributeDiscriminator(TextWriter output)
        {
            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };
            var checkout = new Checkout
            {
                OrderId = 1042,
                Payment = new CardPayment { Amount = 59.90m, LastFour = "4242" },
            };

            string xml = XmlSerializer.Serialize(checkout, options);
            output.WriteLine(xml);

            Checkout? copy = XmlSerializer.Deserialize<Checkout>(xml, options);
            output.WriteLine(copy!.Payment!.GetType().Name);

            try
            {
                XmlSerializer.Deserialize<Checkout>(
                    """<Checkout><OrderId>1042</OrderId><Payment kind="voucher"><Amount>5</Amount></Payment></Checkout>""");
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        #endregion

    }

}
