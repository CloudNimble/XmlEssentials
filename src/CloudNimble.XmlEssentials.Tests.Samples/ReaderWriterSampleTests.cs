using System;
using System.IO;
using System.Threading.Tasks;
using CloudNimble.XmlEssentials.Samples;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests.Samples
{

    /// <summary>
    /// Runs samples 20 to 25 and compares what each prints with the output the documentation shows.
    /// </summary>
    [TestClass]
    public class ReaderWriterSampleTests
    {

        #region Public Methods

        [TestMethod]
        public void Sample20_ReadTokensAndValues()
        {
            SampleOutput.Capture(ReadTokensAndValues.Run).Should().Be(
                "XmlDeclaration (depth 0)\n" +
                "StartElement Order (depth 0)\n" +
                "StartElement Item (depth 1)\n" +
                "Text \"Keyboard\" (depth 2)\n" +
                "EndElement Item (depth 1)\n" +
                "StartElement Gift (depth 1)\n" +
                "StartElement Note (depth 1)\n" +
                "CData \"Leave at <door>\" (depth 2)\n" +
                "EndElement Note (depth 1)\n" +
                "EndElement Order (depth 0)");
        }

        [TestMethod]
        public void Sample20_ReadTokensAndValues_ReadAttributes()
        {
            SampleOutput.Capture(ReadTokensAndValues.ReadAttributes).Should().Be(
                "Item with 2 attribute(s)\n" +
                "  Attribute sku = KB-01\n" +
                "  Attribute quantity = 2\n" +
                "  Back on StartElement Item\n" +
                "Item with 1 attribute(s)\n" +
                "  Attribute sku = MS-07\n" +
                "  Back on StartElement Item");
        }

        [TestMethod]
        public void Sample20_ReadTokensAndValues_ReadEmptyElements()
        {
            SampleOutput.Capture(ReadTokensAndValues.ReadEmptyElements).Should().Be(
                "StartElement Order: IsEmptyElement = False, open elements = 1\n" +
                "StartElement Gift: IsEmptyElement = True, open elements = 1\n" +
                "StartElement Note: IsEmptyElement = False, open elements = 2\n" +
                "EndElement Note: open elements = 1\n" +
                "EndElement Order: open elements = 0");
        }

        [TestMethod]
        public void Sample20_ReadTokensAndValues_ReadTypedValues()
        {
            SampleOutput.Capture(ReadTokensAndValues.ReadTypedValues).Should().Be(
                "id: 1042\n" +
                "rush: True\n" +
                "Placed: 2026-03-14T09:30:00.0000000+01:00\n" +
                "Total: 129.90\n" +
                "Quantity is not a number: three");
        }

        [TestMethod]
        public void Sample20_ReadTokensAndValues_ReadValueBytes()
        {
            SampleOutput.Capture(ReadTokensAndValues.ReadValueBytes).Should().Be(
                "Keyboard: 8 bytes, escaped in the source = False, is Keyboard = True\n" +
                "Mouse & pad: 11 bytes, escaped in the source = True, is Keyboard = False");
        }

        [TestMethod]
        public void Sample20_ReadTokensAndValues_ReadNamespaces()
        {
            SampleOutput.Capture(ReadTokensAndValues.ReadNamespaces).Should().Be(
                "Element prefix='' local='feed' namespace='http://www.w3.org/2005/Atom'\n" +
                "  Attribute prefix='' local='xmlns' namespace='http://www.w3.org/2000/xmlns/'\n" +
                "  Attribute prefix='xmlns' local='dc' namespace='http://www.w3.org/2000/xmlns/'\n" +
                "Element prefix='' local='title' namespace='http://www.w3.org/2005/Atom'\n" +
                "  Attribute prefix='' local='lang' namespace=''\n" +
                "Element prefix='dc' local='creator' namespace='http://purl.org/dc/elements/1.1/'\n" +
                "  This is the Dublin Core creator element.");
        }

        [TestMethod]
        public void Sample21_ReadMarkupAndHandleErrors()
        {
            SampleOutput.Capture(ReadMarkupAndHandleErrors.Run).Should().Be(
                "Default options:\n" +
                "  ProcessingInstruction target=xml-stylesheet data=\"href=\"order.xsl\"\"\n" +
                "  StartElement Order\n" +
                "  Text \"\\n  \"\n" +
                "  Comment \" rush \"\n" +
                "  Text \"\\n  \"\n" +
                "  StartElement Note\n" +
                "  CData \"a < b\"\n" +
                "  EndElement Note\n" +
                "  Text \"\\n\"\n" +
                "  EndElement Order\n" +
                "Comments skipped and white space insignificant:\n" +
                "  ProcessingInstruction target=xml-stylesheet data=\"href=\"order.xsl\"\"\n" +
                "  StartElement Order\n" +
                "  StartElement Note\n" +
                "  CData \"a < b\"\n" +
                "  EndElement Note\n" +
                "  EndElement Order");
        }

        [TestMethod]
        public void Sample21_ReadMarkupAndHandleErrors_ReadDeclaration()
        {
            SampleOutput.Capture(ReadMarkupAndHandleErrors.ReadDeclaration).Should().Be(
                "Before the first read: Version10\n" +
                "XmlDeclaration: Version11\n" +
                "XML 1.1 text: one\\ntwo\n" +
                "XML 1.0 text: one\\u0085two (Version10)");
        }

        [TestMethod]
        public void Sample21_ReadMarkupAndHandleErrors_ReadDeclarationInfo()
        {
            SampleOutput.Capture(ReadMarkupAndHandleErrors.ReadDeclarationInfo).Should().Be(
                "Version: 1.1\n" +
                "Encoding: utf-8\n" +
                "Standalone: True\n" +
                "A document without a declaration has one: False");
        }

        [TestMethod]
        public void Sample21_ReadMarkupAndHandleErrors_SkipElements()
        {
            SampleOutput.Capture(ReadMarkupAndHandleErrors.SkipElements).Should().Be(
                "Read Order\n" +
                "Skipped Customer. Now on EndElement Customer at depth 1.\n" +
                "Read Total");
        }

        [TestMethod]
        public void Sample21_ReadMarkupAndHandleErrors_ReadFragment()
        {
            SampleOutput.Capture(ReadMarkupAndHandleErrors.ReadFragment).Should().Be("Keyboard\nMouse");
        }

        [TestMethod]
        public void Sample21_ReadMarkupAndHandleErrors_EnforceLimits()
        {
            SampleOutput.Capture(ReadMarkupAndHandleErrors.EnforceLimits).Should().Be(
                "XmlException: MaxDepth exceeded (2).\n" +
                "XmlException: MaxAttributeCount exceeded (1).\n" +
                "XmlException: MaxNameByteCount exceeded (4).\n" +
                "XmlException: MaxTextByteCount exceeded (8).\n" +
                "XmlException: MaxDocumentByteCount exceeded (64).\n" +
                "Read 7 tokens.");
        }

        [TestMethod]
        public void Sample21_ReadMarkupAndHandleErrors_ReportErrors()
        {
            SampleOutput.Capture(ReadMarkupAndHandleErrors.ReportErrors).Should().Be(
                "Message: End tag does not match start tag.\n" +
                "Line 2, position 23 (byte 30 of the document)\n" +
                "Path: (not set)");
        }

        [TestMethod]
        public void Sample21_ReadMarkupAndHandleErrors_RejectUnsupported()
        {
            SampleOutput.Capture(ReadMarkupAndHandleErrors.RejectUnsupported).Should().Be(
                "XmlException: DTD is not allowed.\n" +
                "XmlException: XmlEssentials is UTF-8 only. Declared encoding 'iso-8859-1' is not supported.\n" +
                "XmlException: Undefined entity.\n" +
                "Read 3 tokens.");
        }

        [TestMethod]
        public void Sample22_ReadFromSequencesAndStreams()
        {
            SampleOutput.Capture(ReadFromSequencesAndStreams.Run).Should().Be(
                "Item: Keyboard\n" +
                "Item: Mouse\n" +
                "Single segment: False\n" +
                "Consumed 54 of 54 bytes");
        }

        [TestMethod]
        public void Sample22_ReadFromSequencesAndStreams_ReadStream()
        {
            SampleOutput.Capture(ReadFromSequencesAndStreams.ReadStream).Should().Be(
                "Order 1042\n" +
                "  Item: Keyboard\n" +
                "  Item: Mouse & pad\n" +
                "Order 1043\n" +
                "  Item: Monitor stand\n" +
                "Read 142 bytes in 8 blocks. The buffer grew to 32 bytes.");
        }

        [TestMethod]
        public void Sample22_ReadFromSequencesAndStreams_ReadWithScratch()
        {
            SampleOutput.Capture(ReadFromSequencesAndStreams.ReadWithScratch).Should().Be(
                "Escaped values found in two passes: 4\n" +
                "Without a scratch buffer, the reader allocated: yes\n" +
                "With a scratch buffer, the reader allocated: 0 bytes");
        }

        [TestMethod]
        public void Sample23_WriteElementsAndValues()
        {
            SampleOutput.Capture(WriteElementsAndValues.Run).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Order id=\"1042\">\n" +
                "  <Customer>Ada Lovelace</Customer>\n" +
                "  <Total currency=\"EUR\">129.90</Total>\n" +
                "  <Notes/>\n" +
                "</Order>");
        }

        [TestMethod]
        public void Sample23_WriteElementsAndValues_WriteToStream()
        {
            SampleOutput.Capture(WriteElementsAndValues.WriteToStream).Should().Be(
                "84 bytes\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Order id=\"1042\"><Item>Keyboard</Item></Order>");
        }

        [TestMethod]
        public void Sample23_WriteElementsAndValues_WriteTypedValues()
        {
            SampleOutput.Capture(WriteElementsAndValues.WriteTypedValues).Should().Be(
                "<Shipment boxes=\"3\" insured=\"true\">\n" +
                "  <Id>3f2504e0-4f89-11d3-9a0c-0305e82c3301</Id>\n" +
                "  <Sent>2026-03-14T09:30:00+01:00</Sent>\n" +
                "  <Due>2026-03-17</Due>\n" +
                "  <Cutoff>17:00:00</Cutoff>\n" +
                "  <Transit>P1DT12H</Transit>\n" +
                "  <WeightKg>2.5</WeightKg>\n" +
                "  <MaxWeightKg>INF</MaxWeightKg>\n" +
                "  <Fragile>false</Fragile>\n" +
                "  <Signature>WE1MIQ==</Signature>\n" +
                "</Shipment>");
        }

        [TestMethod]
        public void Sample23_WriteElementsAndValues_WriteCompleteElements()
        {
            SampleOutput.Capture(WriteElementsAndValues.WriteCompleteElements).Should().Be(
                "<Order>\n" +
                "  <Item>Keyboard</Item>\n" +
                "  <Quantity>2</Quantity>\n" +
                "  <Total>129.90</Total>\n" +
                "  <Rush>true</Rush>\n" +
                "  <Placed>2026-03-14T09:30:00Z</Placed>\n" +
                "  <Signature>WE1MIQ==</Signature>\n" +
                "  <Comment/>\n" +
                "  <Gift/>\n" +
                "  <Discount xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>\n" +
                "  <Notes></Notes>\n" +
                "</Order>");
        }

        [TestMethod]
        public void Sample23_WriteElementsAndValues_ControlLayout()
        {
            SampleOutput.Capture(WriteElementsAndValues.ControlLayout).Should().Be(
                "<Order id=\"1042\" status=\"open\" region=\"EU\">\n" +
                "  <Customer>Ada Lovelace</Customer>\n" +
                "  <Notes />\n" +
                "</Order>\n" +
                "<Order id=\"1042\"\n" +
                "       status=\"open\"\n" +
                "       region=\"EU\">\n" +
                "  <Customer>Ada Lovelace</Customer>\n" +
                "  <Notes />\n" +
                "</Order>\n" +
                "<Order id=\"1042\" status=\"open\"\n" +
                "       region=\"EU\">\n" +
                "  <Customer>Ada Lovelace</Customer>\n" +
                "  <Notes></Notes>\n" +
                "</Order>");
        }

        [TestMethod]
        public void Sample23_WriteElementsAndValues_ChooseEmptyElementForm()
        {
            SampleOutput.Capture(WriteElementsAndValues.ChooseEmptyElementForm).Should().Be(
                "<a>\n" +
                "  <b></b>\n" +
                "  <c></c>\n" +
                "</a>");
        }

        [TestMethod]
        public void Sample23_WriteElementsAndValues_ChooseLineEnding()
        {
            SampleOutput.Capture(WriteElementsAndValues.ChooseLineEnding).Should().Be(
                "<r>a\\r\n" +
                "b</r>");
        }

        [TestMethod]
        public void Sample23_WriteElementsAndValues_PutAttributesOnLines()
        {
            SampleOutput.Capture(WriteElementsAndValues.PutAttributesOnLines).Should().Be(
                "<order a=\"1\"\n" +
                "       b=\"2\"\n" +
                "       c=\"3\">\n" +
                "  <line x=\"1\"\n" +
                "        y=\"2\"/>\n" +
                "</order>\n" +
                "<order a=\"1\" b=\"2\"\n" +
                "       c=\"3\">\n" +
                "  <line x=\"1\" y=\"2\"/>\n" +
                "</order>");
        }

        [TestMethod]
        public void Sample23_WriteElementsAndValues_EscapeValues()
        {
            SampleOutput.Capture(WriteElementsAndValues.EscapeValues).Should().Be(
                "<Note title=\"Tom &amp; &quot;Jerry&quot; &lt;draft>&#x9;line one&#xA;line two\">if (a &lt; b &amp;&amp; b > c) { x = \"y\"; } ]]&gt; done&#xD;\n" +
                "</Note>\n" +
                "Attribute read back unchanged: True\n" +
                "Text read back unchanged: True");
        }

        [TestMethod]
        public void Sample24_WriteNamespacesAndMarkup()
        {
            SampleOutput.Capture(WriteNamespacesAndMarkup.Run).Should().Be(
                "<feed xmlns=\"http://www.w3.org/2005/Atom\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">\n" +
                "  <title>Orders</title>\n" +
                "  <dc:creator>Ada</dc:creator>\n" +
                "  <s:order xmlns:s=\"urn:example:shop\" s:id=\"1042\"/>\n" +
                "  <entry xmlns:ns1=\"urn:example:shop\" ns1:priority=\"high\" lang=\"en\"/>\n" +
                "</feed>");
        }

        [TestMethod]
        public void Sample24_WriteNamespacesAndMarkup_WriteDeclaration()
        {
            SampleOutput.Capture(WriteNamespacesAndMarkup.WriteDeclaration).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Order/>\n" +
                "<Order/>\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><Order/>\n" +
                "<?xml version=\"1.1\" encoding=\"utf-8\"?><Order/>\n" +
                "With a byte order mark, the output starts with: EFBBBF");
        }

        [TestMethod]
        public void Sample24_WriteNamespacesAndMarkup_WriteIndented()
        {
            SampleOutput.Capture(WriteNamespacesAndMarkup.WriteIndented).Should().Be(
                "Not indented:\n" +
                "<Order><Item><Name>Keyboard</Name></Item><Note>Leave with <b>Ada</b> only.</Note></Order>\n" +
                "Indented:\n" +
                "<Order>\n" +
                "  <Item>\n" +
                "    <Name>Keyboard</Name>\n" +
                "  </Item>\n" +
                "  <Note>Leave with <b>Ada</b> only.</Note>\n" +
                "</Order>\n" +
                "Indented with four spaces:\n" +
                "<Order>\n" +
                "    <Item>\n" +
                "        <Name>Keyboard</Name>\n" +
                "    </Item>\n" +
                "    <Note>Leave with <b>Ada</b> only.</Note>\n" +
                "</Order>");
        }

        [TestMethod]
        public void Sample24_WriteNamespacesAndMarkup_WriteMarkup()
        {
            SampleOutput.Capture(WriteNamespacesAndMarkup.WriteMarkup).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<?xml-stylesheet href=\"order.xsl\" type=\"text/xsl\"?>\n" +
                "<!-- Exported by the order service -->\n" +
                "<Order>\n" +
                "  <!-- One line per item -->\n" +
                "  <Script><![CDATA[if (a < b && b > c) { ship(); }]]></Script>\n" +
                "  <Address><City>London</City><Country>UK</Country></Address>\n" +
                "</Order>");
        }

        [TestMethod]
        public void Sample24_WriteNamespacesAndMarkup_WriteVersion11()
        {
            SampleOutput.Capture(WriteNamespacesAndMarkup.WriteVersion11).Should().Be(
                "<?xml version=\"1.1\" encoding=\"utf-8\"?><Note>bell&#x7; next line&#x85;end</Note>\n" +
                "XML 1.0: Illegal character in XML value.");
        }

        [TestMethod]
        public void Sample25_ManageWriterLifetime()
        {
            SampleOutput.Capture(ManageWriterLifetime.Run).Should().Be(
                "Before Flush: pending = 28, committed = 0, in the buffer = 0\n" +
                "After Flush: pending = 0, committed = 28, in the buffer = 28\n" +
                "The buffer holds: <Order><Item>Keyboard</Item>\n" +
                "After the end tag: pending = 0, committed = 36, depth = 0\n" +
                "The buffer holds: <Order><Item>Keyboard</Item></Order>");
        }

        [TestMethod]
        public void Sample25_ManageWriterLifetime_FlushToStreamAsync()
        {
            CaptureAsync(ManageWriterLifetime.FlushToStreamAsync).Should().Be(
                "Order 1042 flushed: the stream holds 26 bytes\n" +
                "Order 1043 flushed: the stream holds 44 bytes\n" +
                "Order 1044 flushed: the stream holds 62 bytes\n" +
                "<Orders><Order id=\"1042\"/><Order id=\"1043\"/><Order id=\"1044\"/></Orders>");
        }

        [TestMethod]
        public void Sample25_ManageWriterLifetime_ReuseWriter()
        {
            SampleOutput.Capture(ManageWriterLifetime.ReuseWriter).Should().Be(
                "<Item>Keyboard</Item> (21 bytes)\n" +
                "<Item>Mouse</Item> (18 bytes)\n" +
                "<Item>Monitor</Item> (20 bytes)\n" +
                "Second buffer: <Done/>");
        }

        [TestMethod]
        public void Sample25_ManageWriterLifetime_WriteEncodedNames()
        {
            SampleOutput.Capture(ManageWriterLifetime.WriteEncodedNames).Should().Be(
                "<Items><Item>Keyboard</Item><Item>Mouse</Item><SoldOut/></Items>\n" +
                "Item is 4 bytes");
        }

        [TestMethod]
        public void Sample25_ManageWriterLifetime_ValidateOutput()
        {
            SampleOutput.Capture(ManageWriterLifetime.ValidateOutput).Should().Be(
                "XmlException: Invalid XML name.\n" +
                "XmlException: Duplicate attribute.\n" +
                "XmlException: Multiple root elements are not allowed.\n" +
                "XmlException: Text is not allowed at the document level.\n" +
                "XmlException: Attributes must be written on an open start tag.\n" +
                "XmlException: Comments must not contain '--' or end with '-'.\n" +
                "XmlException: CDATA must not contain ']]>'.\n" +
                "XmlException: Illegal character in XML value.\n" +
                "XmlException: No element to end.\n" +
                "XmlException: The document has unclosed elements.");
        }

        [TestMethod]
        public void Sample25_ManageWriterLifetime_DisposeAfterAnError()
        {
            SampleOutput.Capture(ManageWriterLifetime.DisposeAfterAnError).Should().Be("Caught: The document has unclosed elements.");
        }

        [TestMethod]
        public void Sample25_ManageWriterLifetime_SkipValidation()
        {
            SampleOutput.Capture(ManageWriterLifetime.SkipValidation).Should().Be(
                "<Order id=\"1042\" id=\"1043\"><2nd item>Fish &amp; chips</2nd item></Order>\n" +
                "Reading it back: Duplicate attribute.");
        }

        [TestMethod]
        public void Sample25_ManageWriterLifetime_EnforceLimits()
        {
            SampleOutput.Capture(ManageWriterLifetime.EnforceLimits).Should().Be(
                "XmlException: MaxDepth exceeded (2).\n" +
                "XmlException: MaxAttributeCount exceeded (1).\n" +
                "XmlException: MaxNameByteCount exceeded (4).");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Runs an asynchronous sample to its end and returns what it printed.
        /// </summary>
        /// <param name="sample">The sample's entry point.</param>
        /// <returns>Everything the sample printed, without the final line end.</returns>
        private static string CaptureAsync(Func<TextWriter, Task> sample)
        {
            return SampleOutput.Capture(output => sample(output).GetAwaiter().GetResult());
        }

        #endregion

    }

}
