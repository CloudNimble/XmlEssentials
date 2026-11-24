using System;
using System.IO;
using System.Threading.Tasks;
using CloudNimble.XmlEssentials.Samples;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests.Samples
{

    /// <summary>
    /// Runs samples 40 to 45 and compares what each returns with the output the migration guides show.
    /// </summary>
    [TestClass]
    public class MigrationSampleTests
    {

        #region Public Methods

        [TestMethod]
        public void Sample40_MigrateFromXmlSerializer()
        {
            SampleOutput.Capture(MigrateFromXmlSerializer.Run).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<purchaseOrder id=\"1042\">\n" +
                "  <customer>Contoso</customer>\n" +
                "  <items>\n" +
                "    <item sku=\"KB-01\">Keyboard</item>\n" +
                "    <item sku=\"MS-02\">Mouse</item>\n" +
                "  </items>\n" +
                "</purchaseOrder>\n" +
                "Contoso: 2 lines, note is not set");
        }

        [TestMethod]
        public void Sample40_MigrateFromXmlSerializer_BclDefaults()
        {
            SampleOutput.Capture(MigrateFromXmlSerializer.BclDefaults).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Shipment xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n" +
                "  <Carrier>Contoso Freight</Carrier>\n" +
                "  <Insured>true</Insured>\n" +
                "  <Packages>\n" +
                "    <string>PKG-1</string>\n" +
                "    <string>PKG-2</string>\n" +
                "  </Packages>\n" +
                "  <ShippedOn>2026-03-14T09:30:00Z</ShippedOn>\n" +
                "  <Status>InTransit</Status>\n" +
                "  <Weight>12.50</Weight>\n" +
                "</Shipment>");
        }

        [TestMethod]
        public void Sample40_MigrateFromXmlSerializer_Defaults()
        {
            SampleOutput.Capture(MigrateFromXmlSerializer.Defaults).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Shipment>\n" +
                "  <Carrier>Contoso Freight</Carrier>\n" +
                "  <Insured>true</Insured>\n" +
                "  <Packages>PKG-1</Packages>\n" +
                "  <Packages>PKG-2</Packages>\n" +
                "  <ShippedOn>2026-03-14T09:30:00Z</ShippedOn>\n" +
                "  <Status>InTransit</Status>\n" +
                "  <TrackingNumber xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>\n" +
                "  <Weight>12.50</Weight>\n" +
                "</Shipment>");
        }

        [TestMethod]
        public void Sample40_MigrateFromXmlSerializer_ModernTypes()
        {
            SampleOutput.Capture(MigrateFromXmlSerializer.ModernTypes).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Reservation>\n" +
                "  <Guest>Ada Lovelace</Guest>\n" +
                "  <Nights>3</Nights>\n" +
                "  <Charges>\n" +
                "    <Room>420.00</Room>\n" +
                "    <Breakfast>45.50</Breakfast>\n" +
                "  </Charges>\n" +
                "  <Room>204</Room>\n" +
                "</Reservation>\n" +
                "Ada Lovelace, room 204, 3 nights, 2 charges");
        }

        [TestMethod]
        public void Sample40_MigrateFromXmlSerializer_Polymorphism()
        {
            SampleOutput.Capture(MigrateFromXmlSerializer.Polymorphism).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Payment xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:type=\"CardPayment\">\n" +
                "  <Amount>129.90</Amount>\n" +
                "  <LastFour>4242</LastFour>\n" +
                "</Payment>\n" +
                "CardPayment");
        }

        [TestMethod]
        public void Sample40_MigrateFromXmlSerializer_ReadLegacyDocument()
        {
            SampleOutput.Capture(MigrateFromXmlSerializer.ReadLegacyDocument).Should().Be(
                "Carrier: Contoso Freight\n" +
                "Packages: PKG-1, PKG-2\n" +
                "Shipped: 2026-03-14 09:30:00Z (Utc)\n" +
                "Status: InTransit\n" +
                "Tracking number is null: True");
        }

        [TestMethod]
        public void Sample40_MigrateFromXmlSerializer_SourceGeneration()
        {
            SampleOutput.Capture(MigrateFromXmlSerializer.SourceGeneration).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><OrderLine sku=\"KB-01\">Keyboard</OrderLine>\n" +
                "KB-01: Keyboard");
        }

        [TestMethod]
        public void Sample40_MigrateFromXmlSerializer_Utf16Declaration()
        {
            SampleOutput.Capture(MigrateFromXmlSerializer.Utf16Declaration).Should().Be(
                "Contoso Freight\n" +
                "XmlEssentials is UTF-8 only. Declared encoding 'utf-16' is not supported. Path: /Shipment | LineNumber: 1 | BytePositionInLine: 40.");
        }

        [TestMethod]
        public async Task Sample40_MigrateFromXmlSerializer_Utf8AndStreamsAsync()
        {
            (await CaptureAsync(MigrateFromXmlSerializer.Utf8AndStreamsAsync)).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><purchaseOrder id=\"1042\"><customer>Contoso</customer><items/></purchaseOrder>\n" +
                "From bytes: order 1042\n" +
                "From stream: Contoso");
        }

        [TestMethod]
        public void Sample41_MigrateFromXmlReader()
        {
            SampleOutput.Capture(MigrateFromXmlReader.Run).Should().Be(
                "Item at depth 1 with 2 attributes\n" +
                "  Sku: KB-01\n" +
                "  Discontinued: False\n" +
                "  Name: Keyboard\n" +
                "  Price: 49.90\n" +
                "Item at depth 1 with 2 attributes\n" +
                "  Sku: MS-02\n" +
                "  Discontinued: True\n" +
                "  Name: Mouse\n" +
                "  Price: 19.50");
        }

        [TestMethod]
        public void Sample41_MigrateFromXmlReader_DtdIsRejected()
        {
            SampleOutput.Capture(MigrateFromXmlReader.DtdIsRejected).Should().Be("DTD is not allowed.");
        }

        [TestMethod]
        public void Sample41_MigrateFromXmlReader_ReadFromStream()
        {
            SampleOutput.Capture(MigrateFromXmlReader.ReadFromStream).Should().Be("Total: 129.90");
        }

        [TestMethod]
        public void Sample41_MigrateFromXmlReader_Tokens()
        {
            SampleOutput.Capture(MigrateFromXmlReader.Tokens).Should().Be(
                "XmlDeclaration at depth 0\n" +
                "StartElement at depth 0: Order, empty: False\n" +
                "Text at depth 1: ' '\n" +
                "Comment at depth 1\n" +
                "StartElement at depth 1: Item, empty: True\n" +
                "StartElement at depth 1: Note, empty: False\n" +
                "Text at depth 2: 'Fragile & heavy'\n" +
                "EndElement at depth 1: Note\n" +
                "EndElement at depth 0: Order");
        }

        [TestMethod]
        public void Sample41_MigrateFromXmlReader_WithOptions()
        {
            SampleOutput.Capture(MigrateFromXmlReader.WithOptions).Should().Be(
                "StartElement\n" +
                "StartElement\n" +
                "Text: 'Keyboard'\n" +
                "EndElement\n" +
                "EndElement");
        }

        [TestMethod]
        public void Sample42_MigrateFromXmlWriter()
        {
            SampleOutput.Capture(MigrateFromXmlWriter.Run).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<inv:Inventory xmlns:inv=\"urn:contoso:inventory\">\n" +
                "  <inv:Item sku=\"KB-01\">\n" +
                "    <inv:Name>Keyboard &amp; stand</inv:Name>\n" +
                "    <inv:Price>49.90</inv:Price>\n" +
                "    <inv:InStock>true</inv:InStock>\n" +
                "    <inv:Notes><![CDATA[<b>Ships in 2 days</b>]]></inv:Notes>\n" +
                "  </inv:Item>\n" +
                "</inv:Inventory>");
        }

        [TestMethod]
        public void Sample42_MigrateFromXmlWriter_Fragment()
        {
            SampleOutput.Capture(MigrateFromXmlWriter.Fragment).Should().Be("<Item sku=\"KB-01\">Keyboard</Item>");
        }

        [TestMethod]
        public void Sample42_MigrateFromXmlWriter_UnclosedElementIsRejected()
        {
            SampleOutput.Capture(MigrateFromXmlWriter.UnclosedElementIsRejected).Should().Be("The document has unclosed elements.");
        }

        [TestMethod]
        public void Sample43_MigrateFromXDocument()
        {
            SampleOutput.Capture(MigrateFromXDocument.Run).Should().Be(
                "Shelf: Fiction\n" +
                "  Dune (1965)\n" +
                "  Neuromancer (1984)\n" +
                "Shelf: Science\n" +
                "  A Brief History of Time (1988)\n" +
                "No annex");
        }

        [TestMethod]
        public void Sample43_MigrateFromXDocument_CommentsAndWhitespace()
        {
            SampleOutput.Capture(MigrateFromXDocument.CommentsAndWhitespace).Should().Be(
                "Default: 5 children\n" +
                "<Order>\n" +
                "  <!-- rush -->\n" +
                "  <Item>Keyboard</Item>\n" +
                "</Order>\n" +
                "Skip and Insignificant: 1 child");
        }

        [TestMethod]
        public void Sample43_MigrateFromXDocument_Descendants()
        {
            SampleOutput.Capture(MigrateFromXDocument.Descendants).Should().Be("Dune, Neuromancer, A Brief History of Time");
        }

        [TestMethod]
        public void Sample43_MigrateFromXDocument_Edit()
        {
            SampleOutput.Capture(MigrateFromXDocument.Edit).Should().Be(
                "<Shelf name=\"Classics\"><Book id=\"1\" year=\"1965\"><Title>Dune</Title></Book><Book id=\"4\" year=\"1951\"><Title>Foundation</Title></Book></Shelf>");
        }

        [TestMethod]
        public void Sample43_MigrateFromXDocument_Save()
        {
            SampleOutput.Capture(MigrateFromXDocument.Save).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Order id=\"1042\">\n" +
                "  <Item>Keyboard</Item>\n" +
                "  <Item>Mouse</Item>\n" +
                "</Order>");
        }

        [TestMethod]
        public void Sample44_ComingFromSystemTextJson()
        {
            SampleOutput.Capture(ComingFromSystemTextJson.Run).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<catalogItem>\n" +
                "  <displayName>Desk lamp</displayName>\n" +
                "  <id>7</id>\n" +
                "  <inStock>true</inStock>\n" +
                "  <tags>office</tags>\n" +
                "  <tags>lighting</tags>\n" +
                "  <unitPrice>34.95</unitPrice>\n" +
                "</catalogItem>\n" +
                "Desk lamp: 2 tags");
        }

        [TestMethod]
        public void Sample44_ComingFromSystemTextJson_NullAndEmpty()
        {
            SampleOutput.Capture(ComingFromSystemTextJson.NullAndEmpty).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<CatalogItem>\n" +
                "  <Description xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>\n" +
                "  <DisplayName></DisplayName>\n" +
                "  <Id>7</Id>\n" +
                "  <InStock>false</InStock>\n" +
                "  <UnitPrice>0</UnitPrice>\n" +
                "</CatalogItem>\n" +
                "Description is null: True\n" +
                "DisplayName is empty: True");
        }

        [TestMethod]
        public void Sample44_ComingFromSystemTextJson_TextValues()
        {
            SampleOutput.Capture(ComingFromSystemTextJson.TextValues).Should().Be(
                "Count is Element: text '3', number 4\n" +
                "Active is Element: text 'true', Boolean True\n" +
                "sensor is Attribute: text 'A7'");
        }

        [TestMethod]
        public void Sample44_ComingFromSystemTextJson_XmlShapes()
        {
            SampleOutput.Capture(ComingFromSystemTextJson.XmlShapes).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<article xmlns=\"urn:contoso:blog\" id=\"12\">\n" +
                "  <author email=\"ada@contoso.com\">Ada</author>\n" +
                "  <tags>\n" +
                "    <tag>xml</tag>\n" +
                "    <tag>dotnet</tag>\n" +
                "  </tags>\n" +
                "  <headline>Reading XML as UTF-8</headline>\n" +
                "</article>");
        }

        [TestMethod]
        public void Sample45_SystemTextXmlProposal()
        {
            SampleOutput.Capture(SystemTextXmlProposal.Run).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Person><Age>30</Age><Name>Alice</Name></Person>\n" +
                "Alice is 30\n" +
                "Bob is 0\n" +
                "Alice is 30");
        }

        [TestMethod]
        public void Sample45_SystemTextXmlProposal_NamespaceMapping()
        {
            SampleOutput.Capture(SystemTextXmlProposal.NamespaceMapping).Should().Be(
                "<Person xmlns=\"http://example.com/person\" age=\"30\">\n" +
                "  <Address xmlns=\"http://example.com/address\">\n" +
                "    <City>Seattle</City>\n" +
                "    <Country>US</Country>\n" +
                "  </Address>\n" +
                "  <FullName>Alice</FullName>\n" +
                "</Person>");
        }

        [TestMethod]
        public void Sample45_SystemTextXmlProposal_Polymorphism()
        {
            SampleOutput.Capture(SystemTextXmlProposal.Polymorphism).Should().Be(
                "<Catalog>\n" +
                "  <Items>\n" +
                "    <Book>\n" +
                "      <Title>Clean Code</Title>\n" +
                "      <Author>Robert C. Martin</Author>\n" +
                "    </Book>\n" +
                "    <DVD>\n" +
                "      <Title>The Matrix</Title>\n" +
                "      <Runtime>136</Runtime>\n" +
                "    </DVD>\n" +
                "  </Items>\n" +
                "</Catalog>\n" +
                "Book, Dvd");
        }

        [TestMethod]
        public void Sample45_SystemTextXmlProposal_ReadInChunks()
        {
            SampleOutput.Capture(SystemTextXmlProposal.ReadInChunks).Should().Be(
                "Token: StartElement, Depth: 0, Consumed: 17\n" +
                "Token: StartElement, Depth: 1, Consumed: 23\n" +
                "Token: Text, Depth: 2, Consumed: 8\n" +
                "Token: EndElement, Depth: 1, Consumed: 15\n" +
                "Token: StartElement, Depth: 1, Consumed: 21\n" +
                "Token: Text, Depth: 2, Consumed: 5\n" +
                "Token: EndElement, Depth: 1, Consumed: 12\n" +
                "Token: EndElement, Depth: 0, Consumed: 20");
        }

        [TestMethod]
        public void Sample45_SystemTextXmlProposal_ReadSequence()
        {
            SampleOutput.Capture(SystemTextXmlProposal.ReadSequence).Should().Be("Carol is 41");
        }

        [TestMethod]
        public void Sample45_SystemTextXmlProposal_ReaderNamespaces()
        {
            SampleOutput.Capture(SystemTextXmlProposal.ReaderNamespaces).Should().Be("value");
        }

        [TestMethod]
        public void Sample45_SystemTextXmlProposal_SourceGeneration()
        {
            SampleOutput.Capture(SystemTextXmlProposal.SourceGeneration).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><OrderLine sku=\"KB-01\">Keyboard</OrderLine>\n" +
                "KB-01: Keyboard\n" +
                "KB-01: Keyboard");
        }

        [TestMethod]
        public void Sample45_SystemTextXmlProposal_WriterNamespaces()
        {
            SampleOutput.Capture(SystemTextXmlProposal.WriterNamespaces).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<root xmlns:ns=\"http://example.com\" xmlns=\"http://example.com/default\">\n" +
                "  <ns:Child ns:id=\"42\">Hello</ns:Child>\n" +
                "</root>");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Runs an asynchronous <paramref name="sample"/> against a writer that ends lines with a line feed.
        /// </summary>
        /// <param name="sample">The sample's entry point.</param>
        /// <returns>Everything the sample printed, without the final line end.</returns>
        private static async Task<string> CaptureAsync(Func<TextWriter, Task> sample)
        {
            using var writer = new StringWriter { NewLine = "\n" };
            await sample(writer);
            var text = writer.ToString();
            return text.EndsWith('\n') ? text[..^1] : text;
        }

        #endregion

    }

}
