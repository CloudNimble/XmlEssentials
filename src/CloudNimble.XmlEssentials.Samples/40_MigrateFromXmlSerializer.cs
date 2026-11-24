using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Migration;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 40: the <see cref="XmlSerializer"/> code that replaces <c>System.Xml.Serialization.XmlSerializer</c> code.
    /// </summary>
    public static class MigrateFromXmlSerializer
    {

        #region Public Methods

        /// <summary>
        /// Serializes a <see cref="Shipment"/> that has no mapping attributes, to show what the defaults write.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Defaults(TextWriter output)
        {
            var shipment = new Shipment
            {
                Carrier = "Contoso Freight",
                Insured = true,
                Packages = ["PKG-1", "PKG-2"],
                ShippedOn = new DateTime(2026, 3, 14, 9, 30, 0, DateTimeKind.Utc),
                Status = ShipmentStatus.InTransit,
                TrackingNumber = null,
                Weight = 12.50m,
            };

            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };
            output.WriteLine(XmlSerializer.Serialize(shipment, options));
        }

        /// <summary>
        /// Serializes and deserializes an internal record that has a constructor, a required member, an init-only member, and a dictionary.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ModernTypes(TextWriter output)
        {
            var reservation = new Reservation("Ada Lovelace", 3)
            {
                Room = "204",
                Charges = new Dictionary<string, decimal> { ["Room"] = 420.00m, ["Breakfast"] = 45.50m },
            };

            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };
            string xml = XmlSerializer.Serialize(reservation, options);
            output.WriteLine(xml);

            Reservation? copy = XmlSerializer.Deserialize<Reservation>(xml, options);
            output.WriteLine($"{copy!.Guest}, room {copy.Room}, {copy.Nights} nights, {copy.Charges.Count} charges");
        }

        /// <summary>
        /// Reads a document that <c>System.Xml.Serialization.XmlSerializer</c> wrote with its defaults.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadLegacyDocument(TextWriter output)
        {
            const string xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <Shipment xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
                  <Carrier>Contoso Freight</Carrier>
                  <Insured>true</Insured>
                  <Packages>
                    <string>PKG-1</string>
                    <string>PKG-2</string>
                  </Packages>
                  <ShippedOn>2026-03-14T09:30:00Z</ShippedOn>
                  <Status>InTransit</Status>
                  <Weight>12.50</Weight>
                </Shipment>
                """;

            ArchivedShipment? shipment = XmlSerializer.Deserialize<ArchivedShipment>(xml);

            output.WriteLine($"Carrier: {shipment!.Carrier}");
            output.WriteLine($"Packages: {string.Join(", ", shipment.Packages)}");
            output.WriteLine($"Shipped: {shipment.ShippedOn.ToString("u", CultureInfo.InvariantCulture)} ({shipment.ShippedOn.Kind})");
            output.WriteLine($"Status: {shipment.Status}");
            output.WriteLine($"Tracking number is null: {shipment.TrackingNumber is null}");
        }

        /// <summary>
        /// Serializes a derived type through its base type and reads it back as the derived type.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Polymorphism(TextWriter output)
        {
            Payment payment = new CardPayment { Amount = 129.90m, LastFour = "4242" };

            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };
            string xml = XmlSerializer.Serialize(payment, options);
            output.WriteLine(xml);

            Payment? copy = XmlSerializer.Deserialize<Payment>(xml, options);
            output.WriteLine(copy!.GetType().Name);
        }

        /// <summary>
        /// Serializes a <see cref="PurchaseOrder"/> mapped with attributes and deserializes the result.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var order = new PurchaseOrder
            {
                Id = 1042,
                Customer = "Contoso",
                InternalNote = "Call before delivery",
                Lines =
                [
                    new OrderLine { Sku = "KB-01", Description = "Keyboard" },
                    new OrderLine { Sku = "MS-02", Description = "Mouse" },
                ],
            };

            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n" };
            string xml = XmlSerializer.Serialize(order, options);
            output.WriteLine(xml);

            PurchaseOrder? copy = XmlSerializer.Deserialize<PurchaseOrder>(xml, options);
            output.WriteLine($"{copy!.Customer}: {copy.Lines.Count} lines, note is {(copy.InternalNote is null ? "not set" : "set")}");
        }

        /// <summary>
        /// Serializes and deserializes with source-generated metadata in place of reflection.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void SourceGeneration(TextWriter output)
        {
            var line = new OrderLine { Sku = "KB-01", Description = "Keyboard" };

            string xml = XmlSerializer.Serialize(line, MigrationXmlContext.Default.OrderLine);
            output.WriteLine(xml);

            OrderLine? copy = XmlSerializer.Deserialize(xml, MigrationXmlContext.Default.OrderLine);
            output.WriteLine($"{copy!.Sku}: {copy.Description}");
        }

        /// <summary>
        /// Reads a string whose XML declaration names UTF-16, which the old serializer writes to a <see cref="StringWriter"/>,
        /// and shows that the same declaration in UTF-8 bytes is an error.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Utf16Declaration(TextWriter output)
        {
            const string xml = """<?xml version="1.0" encoding="utf-16"?><Shipment><Carrier>Contoso Freight</Carrier></Shipment>""";

            Shipment? shipment = XmlSerializer.Deserialize<Shipment>(xml);
            output.WriteLine(shipment!.Carrier);

            try
            {
                XmlSerializer.Deserialize<Shipment>(Encoding.UTF8.GetBytes(xml));
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Serializes to UTF-8 bytes and to a stream, and deserializes from both.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        /// <returns>A task that completes when the sample has run.</returns>
        public static async Task Utf8AndStreamsAsync(TextWriter output)
        {
            var order = new PurchaseOrder { Id = 1042, Customer = "Contoso" };

            byte[] utf8 = XmlSerializer.SerializeToUtf8Bytes(order);
            output.WriteLine(Encoding.UTF8.GetString(utf8));

            PurchaseOrder? fromBytes = XmlSerializer.Deserialize<PurchaseOrder>(utf8);
            output.WriteLine($"From bytes: order {fromBytes!.Id.ToString(CultureInfo.InvariantCulture)}");

            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsync(stream, order);
            stream.Position = 0;

            PurchaseOrder? fromStream = await XmlSerializer.DeserializeAsync<PurchaseOrder>(stream);
            output.WriteLine($"From stream: {fromStream!.Customer}");
        }

        #endregion

    }

}
