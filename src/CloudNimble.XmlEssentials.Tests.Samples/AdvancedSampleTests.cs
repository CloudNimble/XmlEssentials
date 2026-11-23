using CloudNimble.XmlEssentials.Samples;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests.Samples
{

    /// <summary>
    /// Runs samples 31 to 36 and compares what each prints with the output the documentation shows.
    /// </summary>
    [TestClass]
    public class AdvancedSampleTests
    {

        #region Fields

        private const string OrderXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Order Id=\"1042\"><Customer>Ada</Customer><Line><Quantity>1</Quantity><Sku>KB-01</Sku></Line><Line><Quantity>2</Quantity><Sku>MS-02</Sku></Line></Order>";

        #endregion

        #region Public Methods

        [TestMethod]
        public void Sample31_ReflectionVsSourceGeneration()
        {
            SampleOutput.Capture(ReflectionVsSourceGeneration.Run).Should().Be(
                "<Receipt><Number>77</Number><Paid>129.90</Paid></Receipt>\n" +
                "Receipt 77: 129.90\n" +
                "DefaultXmlTypeInfoResolver");
        }

        [TestMethod]
        public void Sample31_ReflectionVsSourceGeneration_Interceptors()
        {
            SampleOutput.Capture(ReflectionVsSourceGeneration.Interceptors).Should().Be(
                "<Shipment><Carrier>Contoso Freight</Carrier><OrderId>1042</OrderId></Shipment>\n" +
                "ShippingXmlContext");
        }

        [TestMethod]
        public void Sample31_ReflectionVsSourceGeneration_SourceGeneration()
        {
            SampleOutput.Capture(ReflectionVsSourceGeneration.SourceGeneration).Should().Be(
                OrderXml + "\n" +
                "Order 1042 for Ada: 2 lines");
        }

        [TestMethod]
        public void Sample32_UseSourceGeneration()
        {
            SampleOutput.Capture(UseSourceGeneration.Run).Should().Be(
                OrderXml + "\n" +
                "Order 1042 for Ada: 2 lines");
        }

        [TestMethod]
        public void Sample32_UseSourceGeneration_NameTheTypeInfoProperty()
        {
            SampleOutput.Capture(UseSourceGeneration.NameTheTypeInfoProperty).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><OrderLine><Quantity>1</Quantity><Sku>KB-01</Sku></OrderLine>");
        }

        [TestMethod]
        public void Sample32_UseSourceGeneration_SetOptionsAtCompileTime()
        {
            SampleOutput.Capture(UseSourceGeneration.SetOptionsAtCompileTime).Should().Be(
                "<order id=\"1042\"><customer>Ada</customer><Line><quantity>1</quantity><sku>KB-01</sku></Line><Line><quantity>2</quantity><sku>MS-02</sku></Line></order>");
        }

        [TestMethod]
        public void Sample32_UseSourceGeneration_SetOptionsAtRunTime()
        {
            SampleOutput.Capture(UseSourceGeneration.SetOptionsAtRunTime).Should().Be(
                "<Order Id=\"1042\">\n" +
                "  <Customer>Ada</Customer>\n" +
                "  <Line>\n" +
                "    <Quantity>1</Quantity>\n" +
                "    <Sku>KB-01</Sku>\n" +
                "  </Line>\n" +
                "  <Line>\n" +
                "    <Quantity>2</Quantity>\n" +
                "    <Sku>MS-02</Sku>\n" +
                "  </Line>\n" +
                "</Order>");
        }

        [TestMethod]
        public void Sample32_UseSourceGeneration_UseContextOverloads()
        {
            SampleOutput.Capture(UseSourceGeneration.UseContextOverloads).Should().Be(
                OrderXml + "\n" +
                "Order 1042 for Ada: 2 lines");
        }

        [TestMethod]
        public void Sample32_UseSourceGeneration_UseSeveralContexts()
        {
            SampleOutput.Capture(UseSourceGeneration.UseSeveralContexts).Should().Be(
                OrderXml + "\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Shipment><Carrier>Contoso Freight</Carrier><OrderId>1042</OrderId></Shipment>");
        }

        [TestMethod]
        public void Sample33_SourceGenerationModes()
        {
            SampleOutput.Capture(SourceGenerationModes.Run).Should().Be(
                "Default:  OrderXmlConverter, 0 properties\n" +
                "Metadata: XmlObjectConverter`1, 3 properties\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Order Id=\"7\"><Customer>Grace</Customer></Order>\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Order Id=\"7\"><Customer>Grace</Customer></Order>");
        }

        [TestMethod]
        public void Sample33_SourceGenerationModes_InspectMetadata()
        {
            SampleOutput.Capture(SourceGenerationModes.InspectMetadata).Should().Be(
                "Root element: Order\n" +
                "Id: attribute, Int32\n" +
                "Customer: element, String\n" +
                "Line: element, List`1");
        }

        [TestMethod]
        public void Sample34_NativeAot()
        {
            SampleOutput.Capture(NativeAot.Run).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Order Id=\"1042\"><Customer>Ada</Customer><Line><Quantity>1</Quantity><Sku>KB-01</Sku></Line></Order>\n" +
                "Order 1042 for Ada: 1 line");
        }

        [TestMethod]
        public void Sample34_NativeAot_WriteToStream()
        {
            SampleOutput.Capture(NativeAot.WriteToStream).Should().Be(
                "86 bytes\n" +
                "Order 7 for Grace");
        }

        [TestMethod]
        public void Sample35_CustomConverters()
        {
            SampleOutput.Capture(CustomConverters.Run).Should().Be(
                "<Invoice Issued=\"20261123\"><Due>20261223</Due><Number>INV-204</Number><Total>129.90 USD</Total></Invoice>\n" +
                "INV-204: 129.90 USD, issued 2026-11-23, due 2026-12-23");
        }

        [TestMethod]
        public void Sample35_CustomConverters_RegisterOnType()
        {
            SampleOutput.Capture(CustomConverters.RegisterOnType).Should().Be(
                "<Warehouse><Location><Lat>47.61</Lat><Lon>-122.33</Lon></Location><Name>North</Name></Warehouse>\n" +
                "North: 47.61, -122.33");
        }

        [TestMethod]
        public void Sample35_CustomConverters_UseFactory()
        {
            SampleOutput.Capture(CustomConverters.UseFactory).Should().Be(
                "<OrderTracking><OrderId>1042</OrderId><Priority>rush</Priority><Status>ready-to-ship</Status></OrderTracking>\n" +
                "Order 1042: ReadyToShip, Rush");
        }

        [TestMethod]
        public void Sample35_CustomConverters_UseWithSourceGeneration()
        {
            SampleOutput.Capture(CustomConverters.UseWithSourceGeneration).Should().Be(
                "<Quote><Number>Q-17</Number><Price>89.90 EUR</Price></Quote>\n" +
                "Q-17: 89.90 EUR");
        }

        [TestMethod]
        public void Sample36_GenerateTypesFromXsd()
        {
            SampleOutput.Capture(GenerateTypesFromXsd.Run).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Catalog xmlns=\"urn:example:catalog\" published=\"2026-11-23\">\n" +
                "  <Product sku=\"KB-01\">\n" +
                "    <Name>Keyboard</Name>\n" +
                "    <Price>89.90</Price>\n" +
                "    <Availability>InStock</Availability>\n" +
                "    <Weight>0.8</Weight>\n" +
                "    <Supplier>\n" +
                "      <Name>Contoso</Name>\n" +
                "      <Country>NL</Country>\n" +
                "    </Supplier>\n" +
                "  </Product>\n" +
                "  <Product sku=\"MS-02\">\n" +
                "    <Name>Mouse</Name>\n" +
                "    <Price>20</Price>\n" +
                "    <Availability>BackOrder</Availability>\n" +
                "    <Weight xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>\n" +
                "    <Supplier>\n" +
                "      <Name>Fabrikam</Name>\n" +
                "      <Country>DE</Country>\n" +
                "    </Supplier>\n" +
                "  </Product>\n" +
                "</Catalog>\n" +
                "KB-01: Keyboard, InStock, from Contoso\n" +
                "MS-02: Mouse, BackOrder, from Fabrikam");
        }

        [TestMethod]
        public void Sample36_GenerateTypesFromXsd_UseDefaultContext()
        {
            SampleOutput.Capture(GenerateTypesFromXsd.UseDefaultContext).Should().Be(
                "urn:example:catalog\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Supplier xmlns=\"urn:example:catalog\"><Name>Contoso</Name><Country>NL</Country></Supplier>");
        }

        #endregion

    }

}
