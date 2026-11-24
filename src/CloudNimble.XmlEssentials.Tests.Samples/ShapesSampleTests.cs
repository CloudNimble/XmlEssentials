using CloudNimble.XmlEssentials.Samples;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests.Samples
{

    /// <summary>
    /// Runs samples 14 to 19 and compares what each returns with the output the documentation shows.
    /// </summary>
    [TestClass]
    public class ShapesSampleTests
    {

        #region Fields

        private const string Declaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
        private const string XsiNil = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";

        #endregion

        #region Public Methods

        [TestMethod]
        public void Sample14_Namespaces()
        {
            SampleOutput.Capture(Namespaces.Run).Should().Be(
                Declaration + "\n" +
                "<Shipment xmlns=\"urn:example:shipping\">\n" +
                "  <Id>1042</Id>\n" +
                "  <Carrier>Contoso Freight</Carrier>\n" +
                "</Shipment>\n" +
                "Shipment 1042 travels with Contoso Freight");
        }

        [TestMethod]
        public void Sample14_Namespaces_CompareNames()
        {
            SampleOutput.Capture(Namespaces.CompareNames).Should().Be(
                "b:Invoice\n" +
                "{urn:example:billing}Invoice\n" +
                "Invoice\n" +
                "True\n" +
                "False\n" +
                "xsi:nil is true");
        }

        [TestMethod]
        public void Sample14_Namespaces_ChoosePrefixes()
        {
            SampleOutput.Capture(Namespaces.ChoosePrefixes).Should().Be(
                Declaration + "\n" +
                "<Invoice xmlns=\"urn:example:billing\" xmlns:audit=\"urn:example:audit\" xmlns:ship=\"urn:example:shipping\" number=\"7\" audit:revision=\"2\">\n" +
                "  <Customer>Fabrikam</Customer>\n" +
                "  <ship:Note>Leave at the front desk</ship:Note>\n" +
                "</Invoice>");
        }

        [TestMethod]
        public void Sample14_Namespaces_DeclareOnType()
        {
            SampleOutput.Capture(Namespaces.DeclareOnType).Should().Be(
                "<rss xmlns:itunes=\"http://www.itunes.com/dtds/podcast-1.0.dtd\">\n" +
                "  <itunes:author>Contoso Radio</itunes:author>\n" +
                "  <title>Field Notes</title>\n" +
                "</rss>");
        }

        [TestMethod]
        public void Sample14_Namespaces_DiscoverPrefixes()
        {
            SampleOutput.Capture(Namespaces.DiscoverPrefixes).Should().Be(
                "'' is urn:example:billing\n" +
                "'a' is urn:example:audit\n" +
                "'s' is urn:example:shipping\n" +
                "<Invoice xmlns=\"urn:example:billing\" xmlns:a=\"urn:example:audit\" xmlns:s=\"urn:example:shipping\" number=\"7\" a:revision=\"2\">\n" +
                "  <Customer>Fabrikam</Customer>\n" +
                "  <s:Note>Leave at the front desk</s:Note>\n" +
                "</Invoice>");
        }

        [TestMethod]
        public void Sample14_Namespaces_QualifyMembers()
        {
            SampleOutput.Capture(Namespaces.QualifyMembers).Should().Be(
                Declaration + "\n" +
                "<Invoice xmlns=\"urn:example:billing\" number=\"7\" xmlns:ns1=\"urn:example:audit\" ns1:revision=\"2\">\n" +
                "  <Customer>Fabrikam</Customer>\n" +
                "  <Note xmlns=\"urn:example:shipping\">Leave at the front desk</Note>\n" +
                "</Invoice>");
        }

        [TestMethod]
        public void Sample14_Namespaces_ReadPrefixes()
        {
            SampleOutput.Capture(Namespaces.ReadPrefixes).Should().Be(
                "Invoice 7, revision 2\n" +
                "Fabrikam: Leave at the front desk");
        }

        [TestMethod]
        public void Sample14_Namespaces_RejectWrongNamespace()
        {
            SampleOutput.Capture(Namespaces.RejectWrongNamespace).Should().Be("The root element is not in namespace 'urn:example:billing'. Path: /Invoice | LineNumber: 1 | BytePositionInLine: 48.");
        }

        [TestMethod]
        public void Sample14_Namespaces_UseKnownNamespaces()
        {
            SampleOutput.Capture(Namespaces.UseKnownNamespaces).Should().Be(
                "<rss xmlns:atom=\"http://www.w3.org/2005/Atom\" xmlns:content=\"http://purl.org/rss/1.0/modules/content/\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\"" +
                " xmlns:googleplay=\"http://www.google.com/schemas/play-podcasts/1.0\" xmlns:itunes=\"http://www.itunes.com/dtds/podcast-1.0.dtd\" xmlns:media=\"http://search.yahoo.com/mrss/\"" +
                " xmlns:podcast=\"https://podcastindex.org/namespace/1.0\" xmlns:psc=\"http://podlove.org/simple-chapters\" xmlns:sy=\"http://purl.org/rss/1.0/modules/syndication/\">\n" +
                "  <itunes:author>Contoso Radio</itunes:author>\n" +
                "  <title>Field Notes</title>\n" +
                "</rss>");
        }

        [TestMethod]
        public void Sample15_NullAndNil()
        {
            SampleOutput.Capture(NullAndNil.Run).Should().Be(
                Declaration + "\n" +
                "<Contact>\n" +
                "  <Name>Ada</Name>\n" +
                "  <Nickname " + XsiNil + "/>\n" +
                "  <Age " + XsiNil + "/>\n" +
                "</Contact>\n" +
                "Nickname is null: True\n" +
                "Age is null: True");
        }

        [TestMethod]
        public void Sample15_NullAndNil_EnumValues()
        {
            SampleOutput.Capture(NullAndNil.EnumValues).Should().Be(
                "<Ticket><Id>311</Id><Priority>High</Priority></Ticket>\n" +
                "<Ticket><Id>311</Id><Priority>2</Priority></Ticket>\n" +
                "High");
        }

        [TestMethod]
        public void Sample15_NullAndNil_OmitNulls()
        {
            SampleOutput.Capture(NullAndNil.OmitNulls).Should().Be(
                Declaration + "\n" +
                "<Contact>\n" +
                "  <Name>Ada</Name>\n" +
                "</Contact>");
        }

        [TestMethod]
        public void Sample15_NullAndNil_ReadShapes()
        {
            SampleOutput.Capture(NullAndNil.ReadShapes).Should().Be(
                "Omitted: null\n" +
                "Empty: empty string\n" +
                "Nil: null\n" +
                "Empty number: The value is not a valid integer. Path: /Contact/Age | LineNumber: 1 | BytePositionInLine: 33.");
        }

        [TestMethod]
        public void Sample15_NullAndNil_WriteNullRoot()
        {
            SampleOutput.Capture(NullAndNil.WriteNullRoot).Should().Be(
                Declaration + "<Contact " + XsiNil + "/>\n" +
                "True\n" +
                "True");
        }

        [TestMethod]
        public void Sample16_Polymorphism()
        {
            SampleOutput.Capture(Polymorphism.Run).Should().Be(
                Declaration + "\n" +
                "<Catalog>\n" +
                "  <Book>\n" +
                "    <Title>Clean Code</Title>\n" +
                "    <Author>Robert C. Martin</Author>\n" +
                "  </Book>\n" +
                "  <Disc>\n" +
                "    <Title>The Matrix</Title>\n" +
                "    <Runtime>136</Runtime>\n" +
                "  </Disc>\n" +
                "  <Products>\n" +
                "    <Title>Gift card</Title>\n" +
                "  </Products>\n" +
                "</Catalog>\n" +
                "Book: Clean Code\n" +
                "Dvd: The Matrix\n" +
                "Product: Gift card");
        }

        [TestMethod]
        public void Sample16_Polymorphism_FallBackToAncestor()
        {
            SampleOutput.Capture(Polymorphism.FallBackToAncestor).Should().Be(
                "<Vehicle xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"Truck\"><Make>Northwind</Make><Payload>12000</Payload></Vehicle>\n" +
                "<Vehicle xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"Car\"><Make>Contoso</Make><Doors>2</Doors></Vehicle>\n" +
                "Car");
        }

        [TestMethod]
        public void Sample16_Polymorphism_IncludeNonPublicMember()
        {
            SampleOutput.Capture(Polymorphism.IncludeNonPublicMember).Should().Be("<Account><Name>Operations</Name><LedgerCode>OPS-17</LedgerCode></Account>");
        }

        [TestMethod]
        public void Sample16_Polymorphism_RejectUnknownType()
        {
            SampleOutput.Capture(Polymorphism.RejectUnknownType).Should().Be(
                "The type 'CloudNimble.XmlEssentials.Samples.Models.Shapes.Ebook' is not a declared derived type of 'CloudNimble.XmlEssentials.Samples.Models.Shapes.Product'.");
        }

        [TestMethod]
        public void Sample16_Polymorphism_SerializeDeclaredType()
        {
            SampleOutput.Capture(Polymorphism.SerializeDeclaredType).Should().Be(
                "<Payment kind=\"card\"><Amount>59.90</Amount><LastFour>4242</LastFour></Payment>\n" +
                "<CardPayment kind=\"card\"><Amount>59.90</Amount><LastFour>4242</LastFour></CardPayment>\n" +
                "<CardPayment kind=\"card\"><Amount>59.90</Amount><LastFour>4242</LastFour></CardPayment>");
        }

#if NET11_0_OR_GREATER
        [TestMethod]
        public void Sample16_Polymorphism_InferClosedTypes()
        {
            SampleOutput.Capture(Polymorphism.InferClosedTypes).Should().Be(
                "<Outbox>\n" +
                "  <Messages>\n" +
                "    <EmailNotification>\n" +
                "      <Text>Your order shipped.</Text>\n" +
                "      <Address>ada@example.com</Address>\n" +
                "    </EmailNotification>\n" +
                "    <SmsNotification>\n" +
                "      <Text>Your code is 4242.</Text>\n" +
                "      <Number>+1 555 0100</Number>\n" +
                "    </SmsNotification>\n" +
                "  </Messages>\n" +
                "</Outbox>\n" +
                "EmailNotification\n" +
                "SmsNotification");
        }
#endif

        [TestMethod]
        public void Sample16_Polymorphism_UseAttributeDiscriminator()
        {
            SampleOutput.Capture(Polymorphism.UseAttributeDiscriminator).Should().Be(
                Declaration + "\n" +
                "<Checkout>\n" +
                "  <OrderId>1042</OrderId>\n" +
                "  <Payment kind=\"card\">\n" +
                "    <Amount>59.90</Amount>\n" +
                "    <LastFour>4242</LastFour>\n" +
                "  </Payment>\n" +
                "</Checkout>\n" +
                "CardPayment\n" +
                "Unknown type discriminator 'voucher'. Path: /Checkout/Payment | LineNumber: 1 | BytePositionInLine: 58.");
        }

        [TestMethod]
        public void Sample17_PreserveReferences()
        {
            SampleOutput.Capture(PreserveReferences.Run).Should().Be(
                Declaration + "\n" +
                "<Employee xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\" ns1:id=\"1\">\n" +
                "  <Name>Tyler Stein</Name>\n" +
                "  <Manager " + XsiNil + "/>\n" +
                "  <DirectReports>\n" +
                "    <Employee ns1:id=\"2\">\n" +
                "      <Name>Adrian King</Name>\n" +
                "      <Manager ns1:ref=\"1\"/>\n" +
                "      <DirectReports/>\n" +
                "    </Employee>\n" +
                "  </DirectReports>\n" +
                "</Employee>\n" +
                "True");
        }

        [TestMethod]
        public void Sample17_PreserveReferences_IgnoreCycles()
        {
            SampleOutput.Capture(PreserveReferences.IgnoreCycles).Should().Be(
                Declaration + "\n" +
                "<Employee>\n" +
                "  <Name>Tyler Stein</Name>\n" +
                "  <Manager " + XsiNil + "/>\n" +
                "  <DirectReports>\n" +
                "    <Employee>\n" +
                "      <Name>Adrian King</Name>\n" +
                "      <Manager " + XsiNil + "/>\n" +
                "      <DirectReports/>\n" +
                "    </Employee>\n" +
                "  </DirectReports>\n" +
                "</Employee>\n" +
                "True");
        }

        [TestMethod]
        public void Sample17_PreserveReferences_ShareOneObject()
        {
            SampleOutput.Capture(PreserveReferences.ShareOneObject).Should().Be(
                "<Project xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\" ns1:id=\"1\"><Name>Atlas</Name><Owner ns1:id=\"2\"><Name>Dana Birkby</Name><DirectReports/></Owner><Reviewer ns1:ref=\"2\"/></Project>\n" +
                "True\n" +
                "<Project><Name>Atlas</Name><Owner><Name>Dana Birkby</Name><DirectReports/></Owner><Reviewer><Name>Dana Birkby</Name><DirectReports/></Reviewer></Project>\n" +
                "False");
        }

        [TestMethod]
        public void Sample17_PreserveReferences_UseCustomResolver()
        {
            SampleOutput.Capture(PreserveReferences.UseCustomResolver).Should().Be(
                "True\n" +
                "Dana Birkby\n" +
                "atlas, dana\n" +
                "<Project xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\" ns1:id=\"object-1\"><Name>Atlas</Name><Owner ns1:id=\"object-2\"><Name>Dana Birkby</Name><Manager " + XsiNil + "/><DirectReports/></Owner><Reviewer ns1:ref=\"object-2\"/></Project>");
        }

        [TestMethod]
        public void Sample17_PreserveReferences_WriteCycleWithoutHandler()
        {
            SampleOutput.Capture(PreserveReferences.WriteCycleWithoutHandler).Should().Be(
                "XmlException\n" +
                "MaxDepth exceeded (64). Path: /Employee/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager/DirectReports/Employee/Manager.");
        }

        [TestMethod]
        public void Sample18_MixedContent()
        {
            SampleOutput.Capture(MixedContent.Run).Should().Be(
                "Text: \"Press \"\n" +
                "Element: Key = Ctrl\n" +
                "Text: \" and \"\n" +
                "Element: Key = S\n" +
                "Text: \" to save.\"\n" +
                "<Paragraph>Press <Key>Ctrl</Key> and <Key>S</Key> to save. <Note>Changes are saved to disk.</Note></Paragraph>");
        }

        [TestMethod]
        public void Sample18_MixedContent_KeepUnmappedContent()
        {
            SampleOutput.Capture(MixedContent.KeepUnmappedContent).Should().Be(
                "Theme: Dark\n" +
                "Attribute: version = 2\n" +
                "Attribute: ui:density = compact\n" +
                "Element: Telemetry\n" +
                "Element: Font\n" +
                "<Settings version=\"2\" xmlns:ui=\"urn:example:ui\" ui:density=\"compact\">\n" +
                "  <Theme>Light</Theme>\n" +
                "  <Telemetry enabled=\"false\"/>\n" +
                "  <ui:Font size=\"14\">Cascadia Code</ui:Font>\n" +
                "</Settings>");
        }

        [TestMethod]
        public void Sample18_MixedContent_RejectUnmappedContent()
        {
            SampleOutput.Capture(MixedContent.RejectUnmappedContent).Should().Be(
                "Ada\n" +
                "Unknown element 'Phone'. Path: /Contact | LineNumber: 1 | BytePositionInLine: 33.");
        }

        [TestMethod]
        public void Sample18_MixedContent_WriteCData()
        {
            SampleOutput.Capture(MixedContent.WriteCData).Should().Be(
                "<Script language=\"js\"><![CDATA[if (a < b && b > 0) { run(); }]]></Script>\n" +
                "if (a < b && b > 0) { run(); }");
        }

        [TestMethod]
        public void Sample19_Streaming()
        {
            SampleOutput.Capture(output => Streaming.Run(output).GetAwaiter().GetResult()).Should().Be(
                Declaration + "<SensorReading><Sensor>boiler</Sensor><Value>71.5</Value></SensorReading>\n" +
                "boiler: 71.5");
        }

        [TestMethod]
        public void Sample19_Streaming_Cancel()
        {
            SampleOutput.Capture(output => Streaming.Cancel(output).GetAwaiter().GetResult()).Should().Be(
                "boiler: 71.5\n" +
                "Canceled");
        }

        [TestMethod]
        public void Sample19_Streaming_UsePipe()
        {
            SampleOutput.Capture(output => Streaming.UsePipe(output).GetAwaiter().GetResult()).Should().Be(
                "boiler: 71.5\n" +
                "boiler: 72.25\n" +
                "boiler: 73");
        }

        [TestMethod]
        public void Sample19_Streaming_WriteSequence()
        {
            SampleOutput.Capture(output => Streaming.WriteSequence(output).GetAwaiter().GetResult()).Should().Be(
                Declaration + "<ArrayOfSensorReading>" +
                "<SensorReading><Sensor>boiler</Sensor><Value>71.5</Value></SensorReading>" +
                "<SensorReading><Sensor>boiler</Sensor><Value>72.25</Value></SensorReading>" +
                "<SensorReading><Sensor>boiler</Sensor><Value>73</Value></SensorReading>" +
                "</ArrayOfSensorReading>\n" +
                "boiler: 71.5\n" +
                "boiler: 72.25\n" +
                "boiler: 73");
        }

        [TestMethod]
        public void Sample19_Streaming_WriteTopLevelValues()
        {
            SampleOutput.Capture(output => Streaming.WriteTopLevelValues(output).GetAwaiter().GetResult()).Should().Be(
                Declaration + "<SensorReading><Sensor>boiler</Sensor><Value>71.5</Value></SensorReading>" +
                Declaration + "<SensorReading><Sensor>boiler</Sensor><Value>72.25</Value></SensorReading>" +
                Declaration + "<SensorReading><Sensor>boiler</Sensor><Value>73</Value></SensorReading>\n" +
                "boiler: 71.5\n" +
                "boiler: 72.25\n" +
                "boiler: 73");
        }

        #endregion

    }

}
