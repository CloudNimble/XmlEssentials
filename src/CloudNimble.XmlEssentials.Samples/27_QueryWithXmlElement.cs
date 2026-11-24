using System;
using System.Globalization;
using System.IO;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 27: finds, enumerates, and reads the nodes of a parsed document with <see cref="XmlElement"/>.
    /// </summary>
    public static class QueryWithXmlElement
    {

        #region Public Methods

        /// <summary>
        /// Finds child elements and attributes by name, with the throwing and the non-throwing methods.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            const string xml = """
                <Order id="1042">
                  <Customer>Ada Lovelace</Customer>
                  <Total currency="GBP">129.90</Total>
                </Order>
                """;

            using XmlDocument document = XmlDocument.Parse(xml);
            XmlElement order = document.RootElement;

            output.WriteLine($"Customer: {order.GetChild("Customer").GetString()}");
            output.WriteLine($"Order id: {order.GetAttribute("id").GetInt32()}");

            if (order.TryGetChild("Total", out XmlElement total) && total.TryGetAttribute("currency", out XmlElement currency))
            {
                output.WriteLine($"Total: {total.GetDecimal()} {currency.GetString()}");
            }

            if (!order.TryGetChild("Discount", out _))
            {
                output.WriteLine("No Discount element");
            }

            try
            {
                order.GetChild("Discount");
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }

            try
            {
                order.GetAttribute("status");
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Walks the children and the attributes of an element.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Enumerate(TextWriter output)
        {
            const string xml = """
                <Order id="1042" status="open">
                  <Item sku="KB-1">Keyboard</Item>
                  <Item sku="MS-2">Mouse</Item>
                  <Note/>
                </Order>
                """;

            using XmlDocument document = XmlDocument.Parse(xml);
            XmlElement order = document.RootElement;

            output.WriteLine($"{order.AttributeCount} attributes");
            foreach (XmlElement attribute in order.EnumerateAttributes())
            {
                output.WriteLine($"  {attribute.LocalName} = {attribute.GetString()}");
            }

            foreach (XmlElement child in order.EnumerateChildren())
            {
                if (child.ValueKind is not (XmlValueKind.Element or XmlValueKind.EmptyElement or XmlValueKind.Nil))
                {
                    continue;
                }

                if (child.TryGetAttribute("sku", out XmlElement sku))
                {
                    output.WriteLine($"{child.LocalName} {sku.GetString()}: {child.GetString()}");
                }
                else
                {
                    output.WriteLine($"{child.LocalName} ({child.ValueKind})");
                }
            }
        }

        /// <summary>
        /// Finds elements and attributes by names that are spans of characters and UTF-8 bytes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void NamesAsSpans(TextWriter output)
        {
            const string xml = """<Order id="1042"><Customer>Ada Lovelace</Customer><Total>129.90</Total></Order>""";

            using XmlDocument document = XmlDocument.Parse(xml);
            XmlElement order = document.RootElement;

            ReadOnlySpan<char> wanted = "Customer;Total;Discount";
            foreach (Range part in wanted.Split(';'))
            {
                ReadOnlySpan<char> name = wanted[part];
                output.WriteLine(order.TryGetChild(name, out XmlElement child)
                    ? $"{name}: {child.GetString()}"
                    : $"{name}: not in the order");
            }

            output.WriteLine($"id: {order.GetAttribute("id"u8).GetInt32()}");
        }

        /// <summary>
        /// Finds elements and attributes by namespace URI, and reads the parts of a qualified name.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Namespaces(TextWriter output)
        {
            const string xml = """
                <feed xmlns="http://www.w3.org/2005/Atom" xmlns:dc="http://purl.org/dc/elements/1.1/">
                  <title>Release notes</title>
                  <dc:title>Notes de version</dc:title>
                  <link href="https://example.com/" dc:language="fr"/>
                </feed>
                """;

            const string atom = "http://www.w3.org/2005/Atom";
            const string dc = "http://purl.org/dc/elements/1.1/";

            using XmlDocument document = XmlDocument.Parse(xml);
            XmlElement feed = document.RootElement;

            output.WriteLine($"Atom title: {feed.GetChild("title", atom).GetString()}");
            output.WriteLine($"Dublin Core title: {feed.GetChild("title", dc).GetString()}");
            output.WriteLine($"First title in any namespace: {feed.GetChild("title").GetString()}");

            XmlElement dcTitle = feed.GetChild("title", dc);
            output.WriteLine($"Prefix '{dcTitle.Prefix}', local name '{dcTitle.LocalName}', namespace '{dcTitle.NamespaceUri}'");

            XmlElement link = feed.GetChild("link", atom);
            output.WriteLine($"href: {link.GetAttribute("href", "").GetString()}");
            output.WriteLine($"language: {link.GetAttribute("language", dc).GetString()}");

            output.WriteLine($"feed has {feed.AttributeCount} attributes:");
            foreach (XmlElement attribute in feed.EnumerateAttributes())
            {
                string name = attribute.Prefix.Length == 0 ? attribute.LocalName : $"{attribute.Prefix}:{attribute.LocalName}";
                output.WriteLine($"  {name} = {attribute.GetString()}");
            }
        }

        /// <summary>
        /// Reads an element that is marked with <c>xsi:nil</c>.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Nil(TextWriter output)
        {
            const string xml = """
                <Order xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
                  <ShippedOn xsi:nil="true"/>
                  <Total>129.90</Total>
                </Order>
                """;

            using XmlDocument document = XmlDocument.Parse(xml);
            XmlElement shippedOn = document.RootElement.GetChild("ShippedOn");

            output.WriteLine($"ValueKind: {shippedOn.ValueKind}");
            output.WriteLine($"IsNil: {shippedOn.IsNil}");
            output.WriteLine($"GetString returns null: {shippedOn.GetString() is null}");
            output.WriteLine($"GetInnerText returns an empty string: {shippedOn.GetInnerText().Length == 0}");

            try
            {
                shippedOn.GetDateTimeOffset();
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }

            XmlElement nil = shippedOn.GetAttribute(XmlAttributes.Nil, XmlNamespaces.Xsi);
            output.WriteLine($"{nil.Prefix}:{nil.LocalName} = {nil.GetBoolean()}");
        }

        /// <summary>
        /// Compares the three ways to read an element as text.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void TextAndMarkup(TextWriter output)
        {
            const string xml = """<Note priority="high">Ship <b>today</b>, <![CDATA[before 5 & not later]]>.<?audit checked?></Note>""";

            using XmlDocument document = XmlDocument.Parse(xml);
            XmlElement note = document.RootElement;

            output.WriteLine($"GetString:    {note.GetString()}");
            output.WriteLine($"GetInnerText: {note.GetInnerText()}");
            output.WriteLine($"GetRawXml:    {note.GetRawXml()}");

            foreach (XmlElement child in note.EnumerateChildren())
            {
                if (child.ValueKind is XmlValueKind.ProcessingInstruction)
                {
                    output.WriteLine($"On the processing instruction, GetString is '{child.GetString()}' and GetInnerText is '{child.GetInnerText()}'");
                }
            }

            output.WriteLine($"GetRawXml on an attribute is empty: {note.GetAttribute("priority").GetRawXml().Length == 0}");
        }

        /// <summary>
        /// Reads values that might not convert, without an exception.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void TryTypedValues(TextWriter output)
        {
            const string xml = """
                <Reading sensor="12">
                  <Level>high</Level>
                  <Ratio>0.75</Ratio>
                  <Count>70000</Count>
                </Reading>
                """;

            using XmlDocument document = XmlDocument.Parse(xml);
            XmlElement reading = document.RootElement;

            byte sensor = reading.GetAttribute("sensor").GetByte();
            float ratio = reading.GetChild("Ratio").GetSingle();
            output.WriteLine($"sensor: {sensor}");
            output.WriteLine($"ratio: {ratio.ToString(CultureInfo.InvariantCulture)}");

            XmlElement level = reading.GetChild("Level");
            output.WriteLine(level.TryGetInt32(out int number)
                ? $"level: {number}"
                : $"level: not an integer ('{level.GetString()}')");

            XmlElement count = reading.GetChild("Count");
            output.WriteLine($"count as a short: {count.TryGetInt16(out short small)} ({small})");
            output.WriteLine($"count as a uint: {count.TryGetUInt32(out uint large)} ({large})");
        }

        /// <summary>
        /// Reads element and attribute values as .NET types.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void TypedValues(TextWriter output)
        {
            const string xml = """
                <Shipment id="7c9e6679-7425-40de-944b-e07fc1f90ae7" express="1">
                  <Parcels> 3 </Parcels>
                  <WeightKg>12.5</WeightKg>
                  <Cost>49.90</Cost>
                  <TrackingNumber>940011189922</TrackingNumber>
                  <ShippedOn>2026-03-14T09:30:00Z</ShippedOn>
                  <Signature>SGVsbG8=</Signature>
                  <Carrier>Contoso Freight</Carrier>
                </Shipment>
                """;

            using XmlDocument document = XmlDocument.Parse(xml);
            XmlElement shipment = document.RootElement;

            Guid id = shipment.GetAttribute("id").GetGuid();
            bool express = shipment.GetAttribute("express").GetBoolean();
            int parcels = shipment.GetChild("Parcels").GetInt32();
            double weight = shipment.GetChild("WeightKg").GetDouble();
            decimal cost = shipment.GetChild("Cost").GetDecimal();
            long tracking = shipment.GetChild("TrackingNumber").GetInt64();
            DateTimeOffset shippedOn = shipment.GetChild("ShippedOn").GetDateTimeOffset();
            byte[] signature = shipment.GetChild("Signature").GetBytesFromBase64();

            output.WriteLine($"id: {id}");
            output.WriteLine($"express: {express}");
            output.WriteLine($"parcels: {parcels} (the string is '{shipment.GetChild("Parcels").GetString()}')");
            output.WriteLine($"weight: {weight.ToString(CultureInfo.InvariantCulture)}");
            output.WriteLine($"cost: {cost.ToString(CultureInfo.InvariantCulture)}");
            output.WriteLine($"tracking: {tracking}");
            output.WriteLine($"shipped on: {shippedOn.ToString("yyyy-MM-dd HH:mm zzz", CultureInfo.InvariantCulture)}");
            output.WriteLine($"signature: {signature.Length} bytes");

            try
            {
                shipment.GetChild("Carrier").GetInt32();
            }
            catch (XmlException ex)
            {
                output.WriteLine($"Carrier as an integer: {ex.Message}");
            }
        }

        /// <summary>
        /// Prints the kind of every child node of an element with mixed content.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ValueKinds(TextWriter output)
        {
            const string xml = """
                <Order xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"><!-- packed by hand --><Item>Keyboard</Item><Gift/><Coupon xsi:nil="true"/>Leave at the door<![CDATA[<b>fragile</b>]]><?print label?></Order>
                """;

            using XmlDocument document = XmlDocument.Parse(xml);

            foreach (XmlElement child in document.RootElement.EnumerateChildren())
            {
                string description = child.ValueKind switch
                {
                    XmlValueKind.Element or XmlValueKind.EmptyElement or XmlValueKind.Nil => $"<{child.LocalName}>",
                    XmlValueKind.ProcessingInstruction => $"target '{child.LocalName}', data '{child.GetString()}'",
                    _ => $"'{child.GetString()}'",
                };

                output.WriteLine($"{child.ValueKind,-21} {child.TokenType,-21} {description}");
            }
        }

        #endregion

    }

}
