using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Splits documents at every byte offset and checks that a resumed read reports the same tokens as one pass.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderSplitTests
    {

        #region Public Methods

        [TestMethod]
        [DataRow("<a><b><c><d><e><f><g><h><i><j k=\"v\">deep</j></i></h></g></f></e></d></c></b></a>", DisplayName = "ten levels, no namespaces")]
        [DataRow("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<!--c--><p:r xmlns:p=\"urn:p\" xmlns=\"urn:d\" a=\"1&amp;2\" p:b='x y'>\r\n  <c>text &lt;here&gt; caf\u00E9</c><![CDATA[raw <x>]]><?pi data?>\n  <p:e xmlns:q=\"urn:q\" q:z=\"9\"><q:f/></p:e><d xmlns=\"\">t</d>\n</p:r>\n<!--tail-->", DisplayName = "prefixes, default namespace, undeclaration, CDATA, PI, comments")]
        [DataRow("<r><![CDATA[plain run ] ]] ]>x <&\r\nnext\rline é中😀 a\u0085b\u2028c]]]><!--plain run - x -\r\nnext\rline é中😀 a\u0085b\u2028c--></r>", DisplayName = "CDATA and comment with plain runs, lone terminator characters, and line ends")]
        [DataRow("<?xml version=\"1.1\"?><r><![CDATA[plain run ] ]] ]>x <&\r\nnext\rline é中😀 a\u0085b\u2028c]]]><!--plain run - x -\r\nnext\rline é中😀 a\u0085b\u2028c--></r>", DisplayName = "XML 1.1 CDATA and comment with plain runs, NEL, and LS")]
        [DataRow("<r>\n  <a>1</a>\n  <a>sixteen bytes...</a>\n  <a>seventeen bytes..</a>\n  <a>x\ny a>b tail]]</a>\n  <a>a text that is a good deal longer than the short scan &amp; more</a>\n</r>", DisplayName = "short and long text around the bulk scan")]
        [DataRow("<?xml version=\"1.1\"?><r>\n  <a>1</a>\n  <a>sixteen bytes...</a>\n  <a>seventeen bytes..</a>\n  <a>x\ny a>b tail]]</a>\n  <a>a text that is a good deal longer than the short scan &amp; more</a>\n</r>", DisplayName = "XML 1.1 short and long text around the bulk scan")]
        [DataRow("<r xmlns=\"urn:a\"><x xmlns=\"urn:b\"><y xmlns=\"\"><z/></y><w>1</w></x><v>2</v></r>", DisplayName = "nested default namespaces")]
        [DataRow("<a:r xmlns:a=\"urn:u\" xmlns:b=\"urn:u\"><a:e>1</b:e><b:f a:k=\"v\" b:j=\"w\"/></b:r>", DisplayName = "two prefixes on one namespace")]
        [DataRow("<r xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><e xsi:nil=\"true\"/><f xml:lang=\"en\" xml:space=\"preserve\"> x </f></r>", DisplayName = "xsi and xml prefixes")]
        [DataRow("<r a=\"l1&#10;l2&#9;t\" b='q\"q'>a\rb\r\nc&#13;d&#x1F600;]]&gt;</r>", DisplayName = "character references and line ends")]
        [DataRow("<?xml version=\"1.1\"?><r xmlns:p=\"urn:p\" xmlns:q=\"urn:q\"><a xmlns:p=\"\"><b/></a><p:d/><q:c>&#x1;</q:c></r>", DisplayName = "XML 1.1 prefix undeclaration and restricted character")]
        [DataRow("<\u00E9:\u00FC xmlns:\u00E9=\"urn:n\" \u00E9:\u00F6=\"\u00E4\u4E2D\uD83D\uDE00\">\u00EB\u4E2D\uD83D\uDE00<!--\u00EF\uD83D\uDE00--><?p \u00F1\uD83D\uDE00?><![CDATA[\u00DF\uD83D\uDE00]]><\u00E9:\u4E2D\u00E9/></\u00E9:\u00FC>", DisplayName = "multi-byte characters in every construct")]
        [DataRow("<r xml:space=\"preserve\"> <a> </a> <b xml:space=\"default\"> <c/> </b> <d xml:space=\"other\"> <e/> </d> </r>", DisplayName = "xml:space preserve, default, and an unknown value")]
        [DataRow("<p>a <b>b</b> <i>c</i> </p>", DisplayName = "whitespace between inline elements in mixed content")]
        [DataRow("<r> <c> </c> <d><e/> </d> <f> <g/></f> <h> </h></r>", DisplayName = "whitespace-only leaves next to indentation")]
        [DataRow("<a><b><c><d><e><f><g><h><i><j><k><l><m><n><o><p><q><r><s><t> x <u/> </t></s></r></q></p></o></n></m></l></k></j></i></h></g></f></e></d></c></b></a>", DisplayName = "twenty levels")]
        [DataRow("<a><b><c><d><e><f><g><h><i><j><k><l><m><n><o><p><q><r><s><t><z:u xmlns:z=\"urn:z\"><v/></z:u><w>1</w></t></s></r></q></p></o></n></m></l></k></j></i></h></g></f></e></d></c></b></a>", DisplayName = "first namespace at depth twenty-one")]
        [DataRow("<a:e xmlns:a=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:b=\"http://www.w3.org/2001/XMLSchema-instance\"><a:f>1</b:f></b:e>", DisplayName = "two prefixes on a well-known namespace")]
        [DataRow("<r a=\"é☃\" b='x\"y'>aé☃𝄞z é<c d=\"1\" e='2'/>ü</r>", DisplayName = "multi-byte text and attribute values")]
        [DataRow("<?xml version=\"1.1\"?><e a=\"é☃ü\" b='ü'>é☃ü<c d=\"é\"/>ü</e>", DisplayName = "XML 1.1 multi-byte text and attribute values")]
        [DataRow("<p:r xmlns:p=\"urn:1\" xmlns:q=\"urn:q\"><p:a p:x=\"1\" q:x=\"2\"><p:b xmlns:p=\"urn:2\" p:x=\"3\"/><p:c p:x=\"4\"/></p:a><xml:d xml:lang=\"en\"/></p:r>", DisplayName = "repeated and shadowed prefixes")]
        [DataRow("<a><b><c><d><e><f><g><h><z:i xmlns:z=\"urn:z\"><z:j><z:k><z:l><z:m><z:n><z:o><z:p><z:q><z:r><z:s z:t=\"1\">x</z:s></z:r></z:q></z:p></z:o></z:n></z:m></z:l></z:k></z:j></z:i></h></g></f></e></d></c></b></a>", DisplayName = "namespace from depth nine through depth nineteen")]
        public void Split_AtEveryByte_MatchesSinglePass(string xml)
        {
            var bytes = Encoding.UTF8.GetBytes(xml);
            var options = new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow };
            var expected = XmlReaderHarness.Tokens(bytes, options);
            expected.Should().NotBeEmpty();
            for (var split = 1; split < bytes.Length; split++)
            {
                var actual = XmlReaderHarness.TokensSplitAt(bytes, split, options);
                for (var i = 0; i < expected.Count && i < actual.Count; i++)
                {
                    actual[i].Should().Be(expected[i], "token {0} must not depend on a split at byte {1}", i, split);
                }

                actual.Count.Should().Be(expected.Count, "the token count must not depend on a split at byte {0}", split);
            }
        }

        #endregion

    }

}
