using System;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Advanced;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 35: custom converters, registered through options, through an attribute, and through a factory.
    /// </summary>
    public static class CustomConverters
    {

        #region Public Methods

        /// <summary>
        /// Registers two converters in <see cref="XmlSerializerOptions.Converters"/>: one for a type of the
        /// application, and one that replaces the built-in converter of <see cref="DateOnly"/>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            options.Converters.Add(new MoneyConverter());
            options.Converters.Add(new CompactDateConverter());

            var invoice = new Invoice
            {
                Number = "INV-204",
                Issued = new DateOnly(2026, 11, 23),
                Due = new DateOnly(2026, 12, 23),
                Total = new Money(129.9m, "USD"),
            };

            string xml = XmlSerializer.Serialize(invoice, options);
            output.WriteLine(xml);

            Invoice? copy = XmlSerializer.Deserialize<Invoice>(xml, options);
            output.WriteLine($"{copy!.Number}: {copy.Total.Amount} {copy.Total.Currency}, issued {copy.Issued:yyyy-MM-dd}, due {copy.Due:yyyy-MM-dd}");
        }

        /// <summary>
        /// Uses a converter that the type names with <c>[XmlConverter]</c>, and that reads and writes child elements.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RegisterOnType(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            var warehouse = new Warehouse { Name = "North", Location = new GeoPoint(47.61, -122.33) };

            string xml = XmlSerializer.Serialize(warehouse, options);
            output.WriteLine(xml);

            Warehouse? copy = XmlSerializer.Deserialize<Warehouse>(xml, options);
            output.WriteLine($"{copy!.Name}: {copy.Location.Latitude}, {copy.Location.Longitude}");
        }

        /// <summary>
        /// Registers a factory that creates a converter for each enumeration.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void UseFactory(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            options.Converters.Add(new KebabCaseEnumConverterFactory());

            var tracking = new OrderTracking
            {
                OrderId = 1042,
                Status = OrderStatus.ReadyToShip,
                Priority = Priority.Rush,
            };

            string xml = XmlSerializer.Serialize(tracking, options);
            output.WriteLine(xml);

            OrderTracking? copy = XmlSerializer.Deserialize<OrderTracking>(xml, options);
            output.WriteLine($"Order {copy!.OrderId}: {copy.Status}, {copy.Priority}");
        }

        /// <summary>
        /// Uses a converter with a context generated in metadata mode.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void UseWithSourceGeneration(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            options.Converters.Add(new MoneyConverter());
            var context = new QuoteXmlContext(options);

            var quote = new Quote { Number = "Q-17", Price = new Money(89.9m, "EUR") };

            string xml = XmlSerializer.Serialize(quote, context.Quote);
            output.WriteLine(xml);

            Quote? copy = XmlSerializer.Deserialize(xml, context.Quote);
            output.WriteLine($"{copy!.Number}: {copy.Price.Amount} {copy.Price.Currency}");
        }

        #endregion

    }

}
