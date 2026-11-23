using CloudNimble.XmlEssentials.Samples;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests.Samples
{

    /// <summary>
    /// Runs samples 26 to 30 and compares what each prints with the output the DOM guides show.
    /// </summary>
    [TestClass]
    public class DomSampleTests
    {

        #region Public Methods

        [TestMethod]
        public void Sample26_ParseWithXmlDocument()
        {
            SampleOutput.Capture(ParseWithXmlDocument.Run).Should().Be(
                "string: order 1042\n" +
                "UTF-8 span: order 1043\n" +
                "stream: order 1043\n" +
                "sequence: order 1043");
        }

        [TestMethod]
        public void Sample26_ParseWithXmlDocument_Comments()
        {
            SampleOutput.Capture(ParseWithXmlDocument.Comments).Should().Be(
                "Allow (the default): 2 children\n" +
                "  Comment: ' rush '\n" +
                "  Element: 'Keyboard'\n" +
                "Skip: 1 child\n" +
                "Disallow: Comments are not allowed.");
        }

        [TestMethod]
        public void Sample26_ParseWithXmlDocument_Declaration()
        {
            SampleOutput.Capture(ParseWithXmlDocument.Declaration).Should().Be(
                "version 1.1, encoding utf-8, standalone True\n" +
                "has a declaration: False");
        }

        [TestMethod]
        public void Sample26_ParseWithXmlDocument_Lifetime()
        {
            SampleOutput.Capture(ParseWithXmlDocument.Lifetime).Should().Be(
                "The copied string is still valid: Keyboard\n" +
                "The element is not: ObjectDisposedException\n" +
                "A default element is Undefined");
        }

        [TestMethod]
        public void Sample26_ParseWithXmlDocument_Limits()
        {
            SampleOutput.Capture(ParseWithXmlDocument.Limits).Should().Be(
                "MaxDepth exceeded (2).\n" +
                "MaxDocumentByteCount exceeded (64).");
        }

        [TestMethod]
        public void Sample26_ParseWithXmlDocument_ParseValue()
        {
            SampleOutput.Capture(ParseWithXmlDocument.ParseValue).Should().Be(
                "1: First post\n" +
                "2: Second post");
        }

        [TestMethod]
        public void Sample26_ParseWithXmlDocument_TryParse()
        {
            SampleOutput.Capture(ParseWithXmlDocument.TryParse).Should().Be(
                "Parsed: Order\n" +
                "Not well formed: document is null: True");
        }

        [TestMethod]
        public void Sample26_ParseWithXmlDocument_Whitespace()
        {
            SampleOutput.Capture(ParseWithXmlDocument.Whitespace).Should().Be(
                "Significant (the default): 5 children\n" +
                "  Text\n" +
                "  Element\n" +
                "  Text\n" +
                "  Element\n" +
                "  Text\n" +
                "  Text of the root: '\\n  Keyboard\\n  Mouse\\n'\n" +
                "  Text of the first Item: 'Keyboard'\n" +
                "Insignificant: 2 children\n" +
                "  Text of the root: 'KeyboardMouse'");
        }

        [TestMethod]
        public void Sample27_QueryWithXmlElement()
        {
            SampleOutput.Capture(QueryWithXmlElement.Run).Should().Be(
                "Customer: Ada Lovelace\n" +
                "Order id: 1042\n" +
                "Total: 129.90 GBP\n" +
                "No Discount element\n" +
                "Child element 'Discount' was not found.\n" +
                "Attribute 'status' was not found.");
        }

        [TestMethod]
        public void Sample27_QueryWithXmlElement_Enumerate()
        {
            SampleOutput.Capture(QueryWithXmlElement.Enumerate).Should().Be(
                "2 attributes\n" +
                "  id = 1042\n" +
                "  status = open\n" +
                "Item KB-1: Keyboard\n" +
                "Item MS-2: Mouse\n" +
                "Note (EmptyElement)");
        }

        [TestMethod]
        public void Sample27_QueryWithXmlElement_Namespaces()
        {
            SampleOutput.Capture(QueryWithXmlElement.Namespaces).Should().Be(
                "Atom title: Release notes\n" +
                "Dublin Core title: Notes de version\n" +
                "First title in any namespace: Release notes\n" +
                "Prefix 'dc', local name 'title', namespace 'http://purl.org/dc/elements/1.1/'\n" +
                "href: https://example.com/\n" +
                "language: fr\n" +
                "feed has 2 attributes:\n" +
                "  xmlns = http://www.w3.org/2005/Atom\n" +
                "  xmlns:dc = http://purl.org/dc/elements/1.1/");
        }

        [TestMethod]
        public void Sample27_QueryWithXmlElement_Nil()
        {
            SampleOutput.Capture(QueryWithXmlElement.Nil).Should().Be(
                "ValueKind: Nil\n" +
                "IsNil: True\n" +
                "GetString returns null: True\n" +
                "GetInnerText returns an empty string: True\n" +
                "The element is nil and has no value.\n" +
                "xsi:nil = True");
        }

        [TestMethod]
        public void Sample27_QueryWithXmlElement_TextAndMarkup()
        {
            SampleOutput.Capture(QueryWithXmlElement.TextAndMarkup).Should().Be(
                "GetString:    Ship today, before 5 & not later.\n" +
                "GetInnerText: Ship today, before 5 & not later.\n" +
                "GetRawXml:    <Note priority=\"high\">Ship <b>today</b>, <![CDATA[before 5 & not later]]>.<?audit checked?></Note>\n" +
                "On the processing instruction, GetString is 'checked' and GetInnerText is ''\n" +
                "GetRawXml on an attribute is empty: True");
        }

        [TestMethod]
        public void Sample27_QueryWithXmlElement_TypedValues()
        {
            SampleOutput.Capture(QueryWithXmlElement.TypedValues).Should().Be(
                "id: 7c9e6679-7425-40de-944b-e07fc1f90ae7\n" +
                "express: True\n" +
                "parcels: 3 (the string is ' 3 ')\n" +
                "weight: 12.5\n" +
                "cost: 49.90\n" +
                "tracking: 940011189922\n" +
                "shipped on: 2026-03-14 09:30 +00:00\n" +
                "signature: 5 bytes\n" +
                "Carrier as an integer: The value is not a valid integer.");
        }

        [TestMethod]
        public void Sample27_QueryWithXmlElement_ValueKinds()
        {
            SampleOutput.Capture(QueryWithXmlElement.ValueKinds).Should().Be(
                "Comment               Comment               ' packed by hand '\n" +
                "Element               StartElement          <Item>\n" +
                "EmptyElement          StartElement          <Gift>\n" +
                "Nil                   StartElement          <Coupon>\n" +
                "Text                  Text                  'Leave at the door'\n" +
                "CData                 CData                 '<b>fragile</b>'\n" +
                "ProcessingInstruction ProcessingInstruction target 'print', data 'label'");
        }

        [TestMethod]
        public void Sample28_WriteAndConvertXmlDocument()
        {
            SampleOutput.Capture(WriteAndConvertXmlDocument.Run).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><!-- exported nightly --><c:Catalog xmlns:c=\"urn:example:catalog\" updated=\"2026-03-14\"><c:Product sku=\"KB-1\">Keyboard</c:Product><c:Discontinued></c:Discontinued><c:Spare/></c:Catalog>\n" +
                "Same as the input: True");
        }

        [TestMethod]
        public void Sample28_WriteAndConvertXmlDocument_CloneToNode()
        {
            SampleOutput.Capture(WriteAndConvertXmlDocument.CloneToNode).Should().Be(
                "<Product sku=\"KB-2\"><Name>Keyboard</Name><Price>49.90</Price></Product>");
        }

        [TestMethod]
        public void Sample28_WriteAndConvertXmlDocument_Declarations()
        {
            SampleOutput.Capture(WriteAndConvertXmlDocument.Declarations).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><Order id=\"1042\"><Item>Keyboard</Item></Order>\n" +
                "<Order id=\"1042\"><Item>Keyboard</Item></Order>\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><Order id=\"1042\"/>");
        }

        [TestMethod]
        public void Sample28_WriteAndConvertXmlDocument_Deserialize()
        {
            SampleOutput.Capture(WriteAndConvertXmlDocument.Deserialize).Should().Be(
                "Keyboard: 49.90\n" +
                "Mouse: 19.90\n" +
                "Monitor: 249.00");
        }

        [TestMethod]
        public void Sample28_WriteAndConvertXmlDocument_Layout()
        {
            SampleOutput.Capture(WriteAndConvertXmlDocument.Layout).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><!-- exported nightly --><Order id=\"1042\" status=\"open\">\n" +
                "  <!-- rush -->\n" +
                "  <Item>Keyboard A</Item>\n" +
                "  <Notes/>\n" +
                "</Order>");
        }

        [TestMethod]
        public void Sample28_WriteAndConvertXmlDocument_SerializeToDocument()
        {
            SampleOutput.Capture(WriteAndConvertXmlDocument.SerializeToDocument).Should().Be(
                "<Product><Name>Keyboard</Name><Price>49.90</Price></Product>\n" +
                "Price as a decimal: 49.90");
        }

        [TestMethod]
        public void Sample28_WriteAndConvertXmlDocument_WriteElement()
        {
            SampleOutput.Capture(WriteAndConvertXmlDocument.WriteElement).Should().Be(
                "<Picked>\n" +
                "  <c:Product xmlns:c=\"urn:example:catalog\" sku=\"MS-2\">Mouse</c:Product>\n" +
                "</Picked>");
        }

        [TestMethod]
        public void Sample29_BuildWithXmlNode()
        {
            SampleOutput.Capture(BuildWithXmlNode.Run).Should().Be(
                "<Order id=\"1042\"><Customer>Ada Lovelace</Customer><Item>Keyboard</Item><Item>Mouse &amp; pad</Item><Notes/></Order>");
        }

        [TestMethod]
        public void Sample29_BuildWithXmlNode_Declaration()
        {
            SampleOutput.Capture(BuildWithXmlNode.Declaration).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
                "<Order id=\"1042\">\n" +
                "  <Item>Keyboard</Item>\n" +
                "</Order>\n" +
                "<?xml version=\"1.0\" encoding=\"utf-8\" standalone=\"yes\"?><Order id=\"1042\"><Item>Keyboard</Item></Order>");
        }

        [TestMethod]
        public void Sample29_BuildWithXmlNode_Namespaces()
        {
            SampleOutput.Capture(BuildWithXmlNode.Namespaces).Should().Be(
                "<feed xmlns=\"http://www.w3.org/2005/Atom\"><title>Release notes</title><dc:creator xmlns:dc=\"http://purl.org/dc/elements/1.1/\">Ada Lovelace</dc:creator><link href=\"https://example.com/\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\" dc:language=\"en\"/></feed>\n" +
                "<feed xmlns=\"http://www.w3.org/2005/Atom\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\"><title>Release notes</title><dc:creator>Ada Lovelace</dc:creator><link href=\"https://example.com/\" dc:language=\"en\"/></feed>\n" +
                "dc:creator is in http://purl.org/dc/elements/1.1/");
        }

        [TestMethod]
        public void Sample29_BuildWithXmlNode_NodeKinds()
        {
            SampleOutput.Capture(BuildWithXmlNode.NodeKinds).Should().Be(
                "<Note><!-- reviewed -->Use &lt;b> &amp; &lt;/b> for bold. <![CDATA[<b>Raw</b> & unescaped]]><?print copies=\"2\"?><Signature/></Note>\n" +
                "XmlCommentNode: Comment\n" +
                "XmlTextNode: Text\n" +
                "XmlCDataNode: CData\n" +
                "XmlProcessingInstructionNode: ProcessingInstruction\n" +
                "XmlElementNode: Element\n" +
                "InnerText: Use <b> & </b> for bold. <b>Raw</b> & unescaped");
        }

        [TestMethod]
        public void Sample29_BuildWithXmlNode_Prefixes()
        {
            SampleOutput.Capture(BuildWithXmlNode.Prefixes).Should().Be(
                "<Item xmlns:ns1=\"urn:example:units\" ns1:unit=\"kg\"/>\n" +
                "Prefixed names require a namespace URI.");
        }

        [TestMethod]
        public void Sample29_BuildWithXmlNode_ToStringOnParts()
        {
            SampleOutput.Capture(BuildWithXmlNode.ToStringOnParts).Should().Be(
                "<!-- reviewed -->\n" +
                "<?print copies=\"2\"?>\n" +
                "<?xml version=\"1.1\" encoding=\"utf-8\"?>\n" +
                "Text: Text is not allowed at the document level.\n" +
                "Attribute: Attributes must be written on an open start tag.");
        }

        [TestMethod]
        public void Sample30_ChangeWithXmlNode()
        {
            SampleOutput.Capture(ChangeWithXmlNode.Run).Should().Be(
                "Customer: Ada Lovelace\n" +
                "Status: open\n" +
                "Discount element found: False\n" +
                "7 child nodes, 3 of them elements, 2 attributes\n" +
                "KB-1: Keyboard\n" +
                "MS-2: Mouse\n" +
                "The first child is a Text node, and its parent is <Order>");
        }

        [TestMethod]
        public void Sample30_ChangeWithXmlNode_Indentation()
        {
            SampleOutput.Capture(ChangeWithXmlNode.Indentation).Should().Be(
                "<Order>\n" +
                "  <Item>Keyboard</Item>\n" +
                "<Item>Mouse</Item></Order>\n" +
                "<Order><Item>Keyboard</Item><Item>Mouse</Item></Order>\n" +
                "<Order>\n" +
                "  <Item>Keyboard</Item>\n" +
                "  <Item>Mouse</Item>\n" +
                "</Order>");
        }

        [TestMethod]
        public void Sample30_ChangeWithXmlNode_AddAndInsert()
        {
            SampleOutput.Capture(ChangeWithXmlNode.AddAndInsert).Should().Be(
                "<Order><Customer>Ada Lovelace</Customer><!-- items --><Item>Keyboard</Item><Item>Mouse</Item></Order>");
        }

        [TestMethod]
        public void Sample30_ChangeWithXmlNode_Attributes()
        {
            SampleOutput.Capture(ChangeWithXmlNode.Attributes).Should().Be(
                "<Order id=\"1043\" status=\"shipped\" carrier=\"Contoso Freight\"/>");
        }

        [TestMethod]
        public void Sample30_ChangeWithXmlNode_DeepClone()
        {
            SampleOutput.Capture(ChangeWithXmlNode.DeepClone).Should().Be(
                "The node already has a parent.\n" +
                "<Order><Item sku=\"KB-1\">Keyboard</Item></Order>\n" +
                "<Backorder><Item sku=\"KB-1B\">Keyboard</Item></Backorder>");
        }

        [TestMethod]
        public void Sample30_ChangeWithXmlNode_ParseKeeps()
        {
            SampleOutput.Capture(ChangeWithXmlNode.ParseKeeps).Should().Be(
                "<Order>\n" +
                "  <!-- rush -->\n" +
                "  <Item> Keyboard </Item>\n" +
                "  <Notes/>\n" +
                "</Order>\n" +
                "End tag does not match start tag.");
        }

        [TestMethod]
        public void Sample30_ChangeWithXmlNode_ParseValue()
        {
            SampleOutput.Capture(ChangeWithXmlNode.ParseValue).Should().Be(
                "<Entry id=\"1\" read=\"true\"><Title>First post</Title></Entry>\n" +
                "<Entry id=\"2\" read=\"true\"><Title>Second post</Title></Entry>");
        }

        [TestMethod]
        public void Sample30_ChangeWithXmlNode_RemoveAndReplace()
        {
            SampleOutput.Capture(ChangeWithXmlNode.RemoveAndReplace).Should().Be(
                "The removed node has a parent: False\n" +
                "<Order><Item>Trackball (wireless)</Item><Item>Monitor</Item></Order>\n" +
                "<Archive><Note>Gift</Note></Archive>");
        }

        [TestMethod]
        public void Sample30_ChangeWithXmlNode_Serializer()
        {
            SampleOutput.Capture(ChangeWithXmlNode.Serializer).Should().Be(
                "<Product><Name>Keyboard</Name><Price>49.90</Price></Product>\n" +
                "Keyboard: 44.90");
        }

        #endregion

    }

}
