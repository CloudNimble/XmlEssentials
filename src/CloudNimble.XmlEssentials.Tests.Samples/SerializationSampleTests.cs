using CloudNimble.XmlEssentials.Samples;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests.Samples
{

    /// <summary>
    /// Runs samples 6 to 13 and compares what each prints with the output the documentation shows.
    /// </summary>
    [TestClass]
    public class SerializationSampleTests
    {

        #region Public Methods

        [TestMethod]
        public void Sample06_SerializeToXml()
        {
            SampleOutput.Capture(SerializeToXml.Run).Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?><PurchaseOrder><Id>1042</Id><Customer>Contoso</Customer><Placed>2026-03-14</Placed><Total>129.90</Total></PurchaseOrder>");
        }

        [TestMethod]
        public void Sample06_SerializeToXml_EscapedText()
        {
            SampleOutput.Capture(SerializeToXml.EscapedText).Should().Be(
                "<InvoiceCustomer><Name>Fabrikam &amp; Sons &lt;West></Name><Email>billing@fabrikam.example</Email></InvoiceCustomer>\n" +
                "Fabrikam & Sons <West>");
        }

        [TestMethod]
        public void Sample06_SerializeToXml_Indented()
        {
            SampleOutput.Capture(SerializeToXml.Indented).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Invoice>\n" +
                "  <Number>7</Number>\n" +
                "  <BillTo>\n" +
                "    <Name>Contoso</Name>\n" +
                "    <Email>ap@contoso.example</Email>\n" +
                "  </BillTo>\n" +
                "  <Lines>\n" +
                "    <Sku>KB-101</Sku>\n" +
                "    <Quantity>2</Quantity>\n" +
                "  </Lines>\n" +
                "  <Lines>\n" +
                "    <Sku>MS-220</Sku>\n" +
                "    <Quantity>1</Quantity>\n" +
                "  </Lines>\n" +
                "</Invoice>");
        }

        [TestMethod]
        public void Sample06_SerializeToXml_Declaration()
        {
            SampleOutput.Capture(SerializeToXml.Declaration).Should().Be(
                "<InvoiceCustomer><Name>Contoso</Name><Email>ap@contoso.example</Email></InvoiceCustomer>\n" +
                "<?xml version=\"1.1\" encoding=\"utf-8\"?><InvoiceCustomer><Name>Contoso</Name><Email>ap@contoso.example</Email></InvoiceCustomer>");
        }

        [TestMethod]
        public void Sample06_SerializeToXml_ToUtf8Bytes()
        {
            SampleOutput.Capture(SerializeToXml.ToUtf8Bytes).Should().Be(
                "126 bytes\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><InvoiceCustomer><Name>Contoso</Name><Email>ap@contoso.example</Email></InvoiceCustomer>");
        }

        [TestMethod]
        public void Sample06_SerializeToXml_ToBufferWriter()
        {
            SampleOutput.Capture(SerializeToXml.ToBufferWriter).Should().Be(
                "126 bytes\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><InvoiceCustomer><Name>Contoso</Name><Email>ap@contoso.example</Email></InvoiceCustomer>");
        }

        [TestMethod]
        public void Sample06_SerializeToXml_ToStream()
        {
            SampleOutput.Capture(SerializeToXml.ToStream).Should().Be(
                "126 bytes\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><InvoiceCustomer><Name>Contoso</Name><Email>ap@contoso.example</Email></InvoiceCustomer>");
        }

        [TestMethod]
        public void Sample06_SerializeToXml_ToWriter()
        {
            SampleOutput.Capture(SerializeToXml.ToWriter).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Customers>\n" +
                "  <InvoiceCustomer>\n" +
                "    <Name>Contoso</Name>\n" +
                "    <Email>ap@contoso.example</Email>\n" +
                "  </InvoiceCustomer>\n" +
                "  <InvoiceCustomer>\n" +
                "    <Name>Fabrikam</Name>\n" +
                "    <Email>billing@fabrikam.example</Email>\n" +
                "  </InvoiceCustomer>\n" +
                "</Customers>");
        }

        [TestMethod]
        public void Sample06_SerializeToXml_RuntimeType()
        {
            SampleOutput.Capture(SerializeToXml.RuntimeType).Should().Be("<InvoiceLine><Sku>KB-101</Sku><Quantity>2</Quantity></InvoiceLine>");
        }

        [TestMethod]
        public void Sample06_SerializeToXml_ToDocument()
        {
            SampleOutput.Capture(SerializeToXml.ToDocument).Should().Be(
                "InvoiceCustomer\n" +
                "ap@contoso.example");
        }

        [TestMethod]
        public void Sample07_DeserializeFromXml()
        {
            SampleOutput.Capture(DeserializeFromXml.Run).Should().Be(
                "Order 1042 for Contoso\n" +
                "Placed 2026-03-14\n" +
                "Total is 129.90: True");
        }

        [TestMethod]
        public void Sample07_DeserializeFromXml_FromUtf8Bytes()
        {
            SampleOutput.Capture(DeserializeFromXml.FromUtf8Bytes).Should().Be("Contoso <ap@contoso.example>");
        }

        [TestMethod]
        public void Sample07_DeserializeFromXml_FromStream()
        {
            SampleOutput.Capture(DeserializeFromXml.FromStream).Should().Be("Contoso <ap@contoso.example>");
        }

        [TestMethod]
        public void Sample07_DeserializeFromXml_FromReader()
        {
            SampleOutput.Capture(DeserializeFromXml.FromReader).Should().Be(
                "Contoso <ap@contoso.example>\n" +
                "Fabrikam <billing@fabrikam.example>");
        }

        [TestMethod]
        public void Sample07_DeserializeFromXml_FromDocument()
        {
            SampleOutput.Capture(DeserializeFromXml.FromDocument).Should().Be("Contoso <ap@contoso.example>");
        }

        [TestMethod]
        public void Sample07_DeserializeFromXml_RuntimeType()
        {
            SampleOutput.Capture(DeserializeFromXml.RuntimeType).Should().Be("2 x KB-101");
        }

        [TestMethod]
        public void Sample07_DeserializeFromXml_MissingAndUnknown()
        {
            SampleOutput.Capture(DeserializeFromXml.MissingAndUnknown).Should().Be(
                "Id: 1042\n" +
                "Customer: Contoso\n" +
                "Placed: 0001-01-01\n" +
                "Total: 0");
        }

        [TestMethod]
        public void Sample07_DeserializeFromXml_RejectUnknown()
        {
            SampleOutput.Capture(DeserializeFromXml.RejectUnknown).Should().Be("Unknown element 'Warehouse'. Path: /PurchaseOrder | LineNumber: 1 | BytePositionInLine: 40.");
        }

        [TestMethod]
        public void Sample07_DeserializeFromXml_NameMatching()
        {
            SampleOutput.Capture(DeserializeFromXml.NameMatching).Should().Be(
                "Exact: 0, (null)\n" +
                "Case-insensitive: 1042, Contoso");
        }

        [TestMethod]
        public void Sample07_DeserializeFromXml_InvalidXml()
        {
            SampleOutput.Capture(DeserializeFromXml.InvalidXml).Should().Be(
                "The root element does not match 'PurchaseOrder'. Path: /PurchaseOrder | LineNumber: 1 | BytePositionInLine: 8.\n" +
                "End tag does not match start tag. Path: /PurchaseOrder/Id | LineNumber: 1 | BytePositionInLine: 40.\n" +
                "The value is not a valid integer. Path: /PurchaseOrder/Id | LineNumber: 1 | BytePositionInLine: 28.");
        }

        [TestMethod]
        public void Sample07_DeserializeFromXml_LocateError()
        {
            SampleOutput.Capture(DeserializeFromXml.LocateError).Should().Be(
                "The value is not a valid integer. Path: /Invoice/Lines[1]/Quantity | LineNumber: 4 | BytePositionInLine: 50.\n" +
                "Path: /Invoice/Lines[1]/Quantity\n" +
                "Line 4, position 50");
        }

        [TestMethod]
        public void Sample08_CustomizeNames()
        {
            SampleOutput.Capture(CustomizeNames.Run).Should().Be(
                "<product sku=\"KB-101\"><title>Keyboard</title><unit-price>49.95</unit-price></product>\n" +
                "KB-101: Keyboard");
        }

        [TestMethod]
        public void Sample08_CustomizeNames_TextContent()
        {
            SampleOutput.Capture(CustomizeNames.TextContent).Should().Be("<Listing><Name>Keyboard</Name><Price Currency=\"USD\">49.95</Price></Listing>");
        }

        [TestMethod]
        public void Sample08_CustomizeNames_ElementOrder()
        {
            SampleOutput.Capture(CustomizeNames.ElementOrder).Should().Be("<Address><Street>1 Main St</Street><City>Redmond</City><PostalCode>98052</PostalCode></Address>");
        }

        [TestMethod]
        public void Sample08_CustomizeNames_NamingPolicy()
        {
            SampleOutput.Capture(CustomizeNames.NamingPolicy).Should().Be(
                "<warehouse warehouseCode=\"SEA-2\"><streetAddress>400 Dock Rd</streetAddress><loadingDocks>12</loadingDocks></warehouse>\n" +
                "SEA-2: 12 docks");
        }

        [TestMethod]
        public void Sample08_CustomizeNames_ElementAndAttributePolicies()
        {
            SampleOutput.Capture(CustomizeNames.ElementAndAttributePolicies).Should().Be("<warehouse warehouse_code=\"SEA-2\"><street-address>400 Dock Rd</street-address><loading-docks>12</loading-docks></warehouse>");
        }

        [TestMethod]
        public void Sample08_CustomizeNames_BuiltInPolicies()
        {
            SampleOutput.Capture(CustomizeNames.BuiltInPolicies).Should().Be(
                "CamelCase:      streetAddress\n" +
                "PascalCase:     StreetAddress\n" +
                "SnakeCaseLower: street_address\n" +
                "SnakeCaseUpper: STREET_ADDRESS\n" +
                "KebabCaseLower: street-address\n" +
                "KebabCaseUpper: STREET-ADDRESS");
        }

        [TestMethod]
        public void Sample08_CustomizeNames_CustomPolicy()
        {
            SampleOutput.Capture(CustomizeNames.CustomPolicy).Should().Be("<warehouse warehousecode=\"SEA-2\"><streetaddress>400 Dock Rd</streetaddress><loadingdocks>12</loadingdocks></warehouse>");
        }

        [TestMethod]
        public void Sample08_CustomizeNames_AttributesByDefault()
        {
            SampleOutput.Capture(CustomizeNames.AttributesByDefault).Should().Be("<PurchaseOrder Id=\"1042\" Customer=\"Contoso\" Placed=\"2026-03-14\" Total=\"129.90\"/>");
        }

        [TestMethod]
        public void Sample08_CustomizeNames_EnumValues()
        {
            SampleOutput.Capture(CustomizeNames.EnumValues).Should().Be(
                "<parcel><trackingNumber>1Z999</trackingNumber><status>InTransit</status></parcel>\n" +
                "<Parcel><TrackingNumber>1Z999</TrackingNumber><Status>1</Status></Parcel>\n" +
                "Delivered, Delivered");
        }

        [TestMethod]
        public void Sample09_IgnoreProperties()
        {
            SampleOutput.Capture(IgnoreProperties.Run).Should().Be(
                "<UserAccount><UserName>ada</UserName><Email>ada@contoso.example</Email></UserAccount>\n" +
                "Password: (null)");
        }

        [TestMethod]
        public void Sample09_IgnoreProperties_Conditions()
        {
            SampleOutput.Capture(IgnoreProperties.Conditions).Should().Be(
                "<UserProfile><DisplayName>Ada</DisplayName><Website xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></UserProfile>\n" +
                "<UserProfile><DisplayName>Ada</DisplayName><Bio>Mathematician</Bio><SignInCount>3</SignInCount><Website>https://contoso.example</Website></UserProfile>");
        }

        [TestMethod]
        public void Sample09_IgnoreProperties_DefaultCondition()
        {
            SampleOutput.Capture(IgnoreProperties.DefaultCondition).Should().Be(
                "<PurchaseOrder><Id>1042</Id><Customer xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Placed>0001-01-01</Placed><Total>0</Total></PurchaseOrder>\n" +
                "<PurchaseOrder><Id>1042</Id><Placed>0001-01-01</Placed><Total>0</Total></PurchaseOrder>\n" +
                "<PurchaseOrder><Id>1042</Id></PurchaseOrder>");
        }

        [TestMethod]
        public void Sample09_IgnoreProperties_ReadOnlyProperties()
        {
            SampleOutput.Capture(IgnoreProperties.ReadOnlyProperties).Should().Be(
                "<Rectangle><Width>4</Width><Height>3</Height><Area>12</Area></Rectangle>\n" +
                "<Rectangle><Width>4</Width><Height>3</Height></Rectangle>\n" +
                "Area: 12");
        }

        [TestMethod]
        public void Sample09_IgnoreProperties_ConditionsOnRead()
        {
            SampleOutput.Capture(IgnoreProperties.ConditionsOnRead).Should().Be("Ada: Mathematician, 3 sign-ins");
        }

        [TestMethod]
        public void Sample09_IgnoreProperties_Fields()
        {
            SampleOutput.Capture(IgnoreProperties.Fields).Should().Be(
                "<Coordinates/>\n" +
                "<Coordinates><Datum>WGS84</Datum><Latitude>47.5</Latitude><Longitude>-122.25</Longitude></Coordinates>\n" +
                "<Coordinates><Latitude>47.5</Latitude><Longitude>-122.25</Longitude></Coordinates>");
        }

        [TestMethod]
        public void Sample10_RequiredProperties()
        {
            SampleOutput.Capture(RequiredProperties.Run).Should().Be(
                "ada@contoso.example chose pro\n" +
                "The required member 'Email' is missing. Path: /Registration | LineNumber: 1 | BytePositionInLine: 46.\n" +
                "The required member 'Plan' is missing. Path: /Registration | LineNumber: 1 | BytePositionInLine: 64.");
        }

        [TestMethod]
        public void Sample10_RequiredProperties_NilAndEmpty()
        {
            SampleOutput.Capture(RequiredProperties.NilAndEmpty).Should().Be(
                "The required member 'Email' cannot be nil. Path: /Registration/Email | LineNumber: 2 | BytePositionInLine: 27.\n" +
                "Email is empty: True");
        }

        [TestMethod]
        public void Sample10_RequiredProperties_ConstructorParameters()
        {
            SampleOutput.Capture(RequiredProperties.ConstructorParameters).Should().Be(
                "SPRING: 10% off\n" +
                "The required constructor parameter 'Code' is missing. Path: /Coupon | LineNumber: 1 | BytePositionInLine: 45.\n" +
                "(null): 25% off");
        }

        [TestMethod]
        public void Sample10_RequiredProperties_UnmappedMembers()
        {
            SampleOutput.Capture(RequiredProperties.UnmappedMembers).Should().Be(
                "Skip: ada@contoso.example chose pro\n" +
                "Disallow: Unknown element 'Coupon'. Path: /Registration | LineNumber: 1 | BytePositionInLine: 73.\n" +
                "Skip: ada@contoso.example chose pro\n" +
                "Disallow: Unknown attribute 'source'. Path: /Registration | LineNumber: 1 | BytePositionInLine: 28.");
        }

        [TestMethod]
        public void Sample11_ImmutableTypes()
        {
            SampleOutput.Capture(ImmutableTypes.Run).Should().Be(
                "<Temperature><Degrees>21.5</Degrees><Unit>C</Unit></Temperature>\n" +
                "True");
        }

        [TestMethod]
        public void Sample11_ImmutableTypes_ParameterizedConstructor()
        {
            SampleOutput.Capture(ImmutableTypes.ParameterizedConstructor).Should().Be("Voltage = 230");
        }

        [TestMethod]
        public void Sample11_ImmutableTypes_ParameterNameCase()
        {
            SampleOutput.Capture(ImmutableTypes.ParameterNameCase).Should().Be(
                "Voltage = 230\n" +
                "The required constructor parameter 'Name' is missing. Path: /Measurement | LineNumber: 1 | BytePositionInLine: 66.");
        }

        [TestMethod]
        public void Sample11_ImmutableTypes_ChosenConstructor()
        {
            SampleOutput.Capture(ImmutableTypes.ChosenConstructor).Should().Be(
                "<DateRange><Start>2026-03-09</Start><End>2026-03-15</End></DateRange>\n" +
                "2026-03-09 to 2026-03-15");
        }

        [TestMethod]
        public void Sample11_ImmutableTypes_InitOnlyProperties()
        {
            SampleOutput.Capture(ImmutableTypes.InitOnlyProperties).Should().Be(
                "db.contoso.example:6432\n" +
                "db.contoso.example:5432");
        }

        [TestMethod]
        public void Sample11_ImmutableTypes_Structs()
        {
            SampleOutput.Capture(ImmutableTypes.Structs).Should().Be(
                "<GeoPoint><Latitude>47.5</Latitude><Longitude>-122.25</Longitude></GeoPoint>\n" +
                "True");
        }

        [TestMethod]
        public void Sample12_SerializeCollections()
        {
            SampleOutput.Capture(SerializeCollections.Run).Should().Be(
                "<Playlist>\n" +
                "  <Name>Focus</Name>\n" +
                "  <Tracks>Blue in Green</Tracks>\n" +
                "  <Tracks>Naima</Tracks>\n" +
                "  <Tracks>Nardis</Tracks>\n" +
                "</Playlist>\n" +
                "3 tracks, last is Nardis");
        }

        [TestMethod]
        public void Sample12_SerializeCollections_Wrapped()
        {
            SampleOutput.Capture(SerializeCollections.Wrapped).Should().Be(
                "<Album>\n" +
                "  <Title>Kind of Blue</Title>\n" +
                "  <Tracks>\n" +
                "    <Track>So What</Track>\n" +
                "    <Track>Freddie Freeloader</Track>\n" +
                "  </Tracks>\n" +
                "</Album>");
        }

        [TestMethod]
        public void Sample12_SerializeCollections_ItemNames()
        {
            SampleOutput.Capture(SerializeCollections.ItemNames).Should().Be(
                "<NewsFeed>\n" +
                "  <Title>Releases</Title>\n" +
                "  <Entry>1.0.0 is out</Entry>\n" +
                "  <Entry>1.0.1 fixes a typo</Entry>\n" +
                "</NewsFeed>");
        }

        [TestMethod]
        public void Sample12_SerializeCollections_DefaultItemName()
        {
            SampleOutput.Capture(SerializeCollections.DefaultItemName).Should().Be("<Scoreboard><Player>Ada</Player><Scores><Int32>90</Int32><Int32>85</Int32></Scores></Scoreboard>");
        }

        [TestMethod]
        public void Sample12_SerializeCollections_EmptyAndNull()
        {
            SampleOutput.Capture(SerializeCollections.EmptyAndNull).Should().Be(
                "<Playlist><Name>Empty</Name></Playlist>\n" +
                "<Album><Title>Empty</Title><Tracks/></Album>\n" +
                "<Album><Title>Null</Title><Tracks xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></Album>\n" +
                "Wrapped and empty: 0\n" +
                "Wrapper missing: null");
        }

        [TestMethod]
        public void Sample12_SerializeCollections_Dictionaries()
        {
            SampleOutput.Capture(SerializeCollections.Dictionaries).Should().Be(
                "<AppSettings>\n" +
                "  <Application>Storefront</Application>\n" +
                "  <Values>\n" +
                "    <Theme>dark</Theme>\n" +
                "    <PageSize>20</PageSize>\n" +
                "    <Key>Support email</Key>\n" +
                "    <Value>help@contoso.example</Value>\n" +
                "  </Values>\n" +
                "</AppSettings>\n" +
                "3 values, support email is help@contoso.example");
        }

        [TestMethod]
        public void Sample12_SerializeCollections_DictionaryKeyPolicy()
        {
            SampleOutput.Capture(SerializeCollections.DictionaryKeyPolicy).Should().Be(
                "<AppSettings><Application>Storefront</Application><Values><pageSize>20</pageSize></Values></AppSettings>\n" +
                "Keys read back: pageSize");
        }

        [TestMethod]
        public void Sample12_SerializeCollections_CollectionTypes()
        {
            SampleOutput.Capture(SerializeCollections.CollectionTypes).Should().Be(
                "<StockReport><Counts>12</Counts><Counts>7</Counts><Warehouses>SEA-2</Warehouses><Warehouses>PDX-1</Warehouses><Skus>KB-101</Skus><Skus>MS-220</Skus><Tags>weekly</Tags><Notes>Recount MS-220</Notes></StockReport>\n" +
                "Counts: 2\n" +
                "Warehouses is a List<string>: True\n" +
                "Skus is a List<string>: True\n" +
                "Tags: 1\n" +
                "Notes: 1");
        }

        [TestMethod]
        public void Sample12_SerializeCollections_RootCollections()
        {
            SampleOutput.Capture(SerializeCollections.RootCollections).Should().Be(
                "<ListOfInvoiceLine><InvoiceLine><Sku>KB-101</Sku><Quantity>2</Quantity></InvoiceLine><InvoiceLine><Sku>MS-220</Sku><Quantity>1</Quantity></InvoiceLine></ListOfInvoiceLine>\n" +
                "2 lines\n" +
                "<Int32Array><Int32>1</Int32><Int32>2</Int32><Int32>3</Int32></Int32Array>\n" +
                "<DictionaryOfStringInt32><Width>4</Width><Height>3</Height></DictionaryOfStringInt32>");
        }

        [TestMethod]
        public void Sample13_ConfigureOptions()
        {
            SampleOutput.Capture(ConfigureOptions.Run).Should().Be(
                "<invoiceLine><sku>KB-101</sku><quantity>2</quantity></invoiceLine>\n" +
                "<invoiceCustomer><name>Contoso</name><email>ap@contoso.example</email></invoiceCustomer>");
        }

        [TestMethod]
        public void Sample13_ConfigureOptions_BuiltInInstances()
        {
            SampleOutput.Capture(ConfigureOptions.BuiltInInstances).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><InvoiceLine><Sku>KB-101</Sku><Quantity>2</Quantity></InvoiceLine>\n" +
                "<invoiceLine><sku>KB-101</sku><quantity>2</quantity></invoiceLine>\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><InvoiceLine><Sku>KB-101</Sku><Quantity>2</Quantity></InvoiceLine>");
        }

        [TestMethod]
        public void Sample13_ConfigureOptions_StrictDefaults()
        {
            SampleOutput.Capture(ConfigureOptions.StrictDefaults).Should().Be(
                "General: MS-220\n" +
                "Strict: Duplicate property 'Sku'. Path: /InvoiceLine | LineNumber: 1 | BytePositionInLine: 36.\n" +
                "General: KB-101\n" +
                "Strict: Comments are not allowed. Path: /InvoiceLine | LineNumber: 1 | BytePositionInLine: 30.\n" +
                "General: KB-101\n" +
                "Strict: Unknown element 'Colour'. Path: /InvoiceLine | LineNumber: 1 | BytePositionInLine: 39.");
        }

        [TestMethod]
        public void Sample13_ConfigureOptions_CopyAndChange()
        {
            SampleOutput.Capture(ConfigureOptions.CopyAndChange).Should().Be(
                "<invoiceLine>\n" +
                "  <sku>KB-101</sku>\n" +
                "  <quantity>2</quantity>\n" +
                "</invoiceLine>\n" +
                "Source is read-only: True. Copy is read-only: False.\n" +
                "<invoiceLine><sku>KB-101</sku><quantity>2</quantity></invoiceLine>");
        }

        [TestMethod]
        public void Sample13_ConfigureOptions_ReadOnly()
        {
            SampleOutput.Capture(ConfigureOptions.ReadOnly).Should().Be(
                "Before first use: False\n" +
                "After first use: True\n" +
                "This XmlSerializerOptions instance is read-only.\n" +
                "After MakeReadOnly: True");
        }

        [TestMethod]
        public void Sample13_ConfigureOptions_Defaults()
        {
            SampleOutput.Capture(ConfigureOptions.Defaults).Should().Be(
                "AllowDuplicateProperties = True\n" +
                "AttributeNamingPolicy = null\n" +
                "AttributeStyle = Inline\n" +
                "AttributesPerLine = 1\n" +
                "Converters.Count = 0\n" +
                "DefaultBufferSize = 0\n" +
                "DefaultIgnoreCondition = Never\n" +
                "DefaultNamespace = null\n" +
                "DefaultPropertyMapping = Element\n" +
                "DictionaryKeyPolicy = null\n" +
                "ElementNamingPolicy = null\n" +
                "EmitByteOrderMark = False\n" +
                "EmptyElementStyle = SelfClosing\n" +
                "EnumHandling = Name\n" +
                "IgnoreReadOnlyFields = False\n" +
                "IgnoreReadOnlyProperties = False\n" +
                "IncludeFields = False\n" +
                "IndentCharacter = ' '\n" +
                "IndentSize = 0\n" +
                "InferClosedTypePolymorphism = False\n" +
                "IsReadOnly = False\n" +
                "MaxAttributeCount = 0\n" +
                "MaxCommentByteCount = 0\n" +
                "MaxDepth = 0\n" +
                "MaxDocumentByteCount = 0\n" +
                "MaxNameByteCount = 0\n" +
                "MaxTextByteCount = 0\n" +
                "NewLine is Environment.NewLine = True\n" +
                "NumberHandling = AllowNamedFloatingPointLiterals\n" +
                "OmitXmlDeclaration = False\n" +
                "PreferredObjectCreationHandling = Replace\n" +
                "PropertyNameCaseInsensitive = False\n" +
                "PropertyNamingPolicy = null\n" +
                "ReadCommentHandling = Skip\n" +
                "ReferenceHandler = null\n" +
                "RespectNullableAnnotations = False\n" +
                "RespectRequiredConstructorParameters = True\n" +
                "TypeInfoResolverChain.Count = 0\n" +
                "UnknownTypeHandling = XmlElement\n" +
                "UnmappedMemberHandling = Skip\n" +
                "WhitespaceHandling = Insignificant\n" +
                "WriteIndented = False\n" +
                "XmlVersion = Version10");
        }

        #endregion

    }

}
