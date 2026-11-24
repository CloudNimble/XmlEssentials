using System;
using System.Diagnostics.CodeAnalysis;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.Aot.Generated;

namespace CloudNimble.XmlEssentials.Tests.Aot
{

    /// <summary>
    /// Native AOT smoke: generated context, XSD-generated types, reflection off, reader walk.
    /// </summary>
    /// <remarks>
    /// Publish with <c>dotnet publish -c Release -p:PublishAot=true</c> and run the native executable.
    /// Exit code 0 means every check matched. Exit code 1 prints the first mismatch.
    /// </remarks>
    public static class Program
    {

        #region Public Methods

        /// <summary>
        /// Runs the AOT smoke checks.
        /// </summary>
        /// <param name="args">Unused.</param>
        /// <returns>0 on success, 1 on failure.</returns>
        public static int Main(string[] args)
        {
            _ = args;
            string personXml;
            string orderXml;
            string inventoryXml;
            try
            {
                CheckReflectionIsOff();
                personXml = CheckPersonRoundTrip();
                orderXml = CheckOrderFromXsd();
                inventoryXml = CheckInventoryRoundTrip();
                CheckReader();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            Console.WriteLine(personXml);
            Console.WriteLine(orderXml);
            Console.WriteLine(inventoryXml);
            Console.WriteLine("XmlEssentials AOT smoke passed.");
            return 0;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Fails when <paramref name="actual"/> is not <paramref name="expected"/>.
        /// </summary>
        /// <param name="actual">The value produced.</param>
        /// <param name="expected">The required value.</param>
        /// <param name="name">The check name.</param>
        internal static void CheckEqual(string actual, string expected, string name)
        {
            if (actual == expected)
            {
                return;
            }

            throw new InvalidOperationException(name + ":" + Environment.NewLine + "expected: " + expected + Environment.NewLine + "actual:   " + actual);
        }

        /// <summary>
        /// Serializes a type whose members the generated converter hands to the runtime library, and reads it back.
        /// </summary>
        /// <returns>The serialized document.</returns>
        /// <remarks>
        /// A dictionary, collections that are not lists, a collection with no setter, an XML value, and a value
        /// declared as object are read and written by the runtime library, not by generated code. This check
        /// proves they work with reflection off and the code trimmed.
        /// </remarks>
        internal static string CheckInventoryRoundTrip()
        {
            const string expected = "<Inventory><Any>5</Any><Codes>1</Codes><Codes>2</Codes><Counts><bolts>40</bolts></Counts><Notes>n</Notes>"
                + "<Pending>7</Pending><Pending>8</Pending><Raw><part id=\"9\"/></Raw><Sizes>3</Sizes><Tags>new</Tags></Inventory>";
            using var document = XmlDocument.Parse("<part id=\"9\"/>");
            var inventory = new Inventory
            {
                Any = 5,
                Codes = [1, 2],
                Counts = { ["bolts"] = 40 },
                Pending = new System.Collections.Generic.Queue<int>([7, 8]),
                Raw = document.RootElement,
                Sizes = [3],
                Tags = ["new"],
            };
            inventory.Notes.Add("n");
            var xml = XmlSerializer.Serialize(inventory, AppXmlContext.Default.Inventory);
            CheckEqual(xml, expected, "inventory");
            var read = XmlSerializer.Deserialize(xml, AppXmlContext.Default.Inventory);
            if (read is null)
            {
                throw new InvalidOperationException("inventory deserialize returned null.");
            }

            CheckEqual(read.Counts["bolts"].ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + read.Pending.Count + "," + read.Sizes.Length + "," + read.Notes.Count + "," + read.Tags.Count + "," + read.Codes.Length, "40,2,1,1,1,2", "inventory-values");
            CheckEqual(((XmlElement)read.Any!).GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture), "5", "inventory-any");
            read.Any = 5;
            CheckEqual(XmlSerializer.Serialize(read, AppXmlContext.Default.Inventory), expected, "inventory-roundtrip");
            return xml;
        }

        /// <summary>
        /// Serializes an XSD-generated order through its generated context and round-trips the same document.
        /// </summary>
        /// <returns>The serialized document.</returns>
        internal static string CheckOrderFromXsd()
        {
            const string expected = "<Order xmlns=\"urn:order\" currency=\"USD\"><Id>A-1</Id><Status>Open</Status><Qty>2</Qty><Line><Sku>sku</Sku><Qty>1</Qty></Line></Order>";
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = "urn:order" };
            var context = new OrderXmlContext(options);
            var order = new Order
            {
                currency = "USD",
                Id = "A-1",
                Status = Status.Open,
                Qty = 2,
                Line = [new Order_Line { Sku = "sku", Qty = 1 }],
            };
            var xml = XmlSerializer.Serialize(order, context.Order);
            CheckEqual(xml, expected, "xsd-order");
            var read = XmlSerializer.Deserialize(xml, context.Order);
            if (read is null)
            {
                throw new InvalidOperationException("xsd-order deserialize returned null.");
            }

            CheckEqual(XmlSerializer.Serialize(read, context.Order), expected, "xsd-order-roundtrip");
            return xml;
        }

        /// <summary>
        /// Serializes a source-generated person through <see cref="AppXmlContext"/> and round-trips the same document.
        /// </summary>
        /// <returns>The serialized document.</returns>
        internal static string CheckPersonRoundTrip()
        {
            const string expected = "<Person><Age>36</Age><Name>Ada</Name></Person>";
            var xml = XmlSerializer.Serialize(new Person { Name = "Ada", Age = 36 }, AppXmlContext.Default.Person);
            CheckEqual(xml, expected, "person");
            var read = XmlSerializer.Deserialize(xml, AppXmlContext.Default.Person);
            if (read is null)
            {
                throw new InvalidOperationException("person deserialize returned null.");
            }

            CheckEqual(XmlSerializer.Serialize(read, AppXmlContext.Default.Person), expected, "person-roundtrip");
            return xml;
        }

        /// <summary>
        /// Walks a tiny document with <see cref="Utf8XmlReader"/>.
        /// </summary>
        internal static void CheckReader()
        {
            var reader = new Utf8XmlReader("<root id=\"1\"><n>42</n></root>"u8);
            var starts = 0;
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.StartElement)
                {
                    starts++;
                }
            }

            if (starts != 2)
            {
                throw new InvalidOperationException("reader start-element count: " + starts);
            }
        }

        /// <summary>
        /// Confirms the feature switch is off and an unregistered type cannot serialize.
        /// </summary>
        /// <remarks>
        /// Calls the reflection <c>Serialize</c> overload on purpose so the smoke proves it throws when the switch is off.
        /// </remarks>
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "This check calls the reflection Serialize overload to prove it throws.")]
        [UnconditionalSuppressMessage("Trim", "IL2026", Justification = "This check calls the reflection Serialize overload to prove it throws.")]
        internal static void CheckReflectionIsOff()
        {
            if (XmlSerializer.IsReflectionEnabledByDefault)
            {
                throw new InvalidOperationException("IsReflectionEnabledByDefault must be false in the AOT smoke.");
            }

            // A generated context does not map a type it does not list: it has no answer for one.
            if (AppXmlContext.Default.GetTypeInfo(typeof(Unregistered)) is not null)
            {
                throw new InvalidOperationException("A generated context must not answer for a type it does not list.");
            }

            try
            {
                XmlSerializer.Serialize(new Unregistered { N = 1 });
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("Serialize of an unregistered type must throw when reflection is disabled.");
        }

        #endregion

    }

}
