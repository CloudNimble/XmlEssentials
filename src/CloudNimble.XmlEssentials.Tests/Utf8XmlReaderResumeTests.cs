using System;
using System.Linq;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// IsFinalBlock resume across splits inside a namespaced start tag, an attribute, and text.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderResumeTests
    {

        #region Public Methods

        [TestMethod]
        public void Resume_BetweenTokensKeepsDepth()
        {
            var first = "<a><b/>"u8.ToArray();
            var second = "</a>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            while (reader.Read())
            {
            }

            reader.CurrentDepth.Should().Be(1);
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.TokenType.Should().Be(XmlTokenType.EndElement);
            resumed.LocalNameEquals("a").Should().BeTrue();
        }

        [TestMethod]
        public void TrySkip_OnAPartialBlock_LeavesTheReaderOnTheStartTag()
        {
            var whole = "<r><a x=\"1\"><b>text</b><c/></a><d/></r>"u8.ToArray();
            var afterA = "<r><a x=\"1\"><b>text</b><c/></a>"u8.Length;
            for (var split = "<r><a x=\"1\">"u8.Length; split < afterA; split++)
            {
                var reader = new Utf8XmlReader(whole.AsSpan(0, split), false, default);
                reader.Read().Should().BeTrue();
                reader.Read().Should().BeTrue();
                var consumed = reader.BytesConsumed;
                reader.TrySkip().Should().BeFalse();
                reader.TokenType.Should().Be(XmlTokenType.StartElement);
                reader.LocalNameEquals("a").Should().BeTrue();
                reader.CurrentDepth.Should().Be(2);
                reader.BytesConsumed.Should().Be(consumed);
                reader.MoveToNextAttribute().Should().BeTrue();
                reader.ValueTextEquals("1").Should().BeTrue();
            }

            var complete = new Utf8XmlReader(whole.AsSpan(0, afterA), false, default);
            complete.Read().Should().BeTrue();
            complete.Read().Should().BeTrue();
            complete.TrySkip().Should().BeTrue();
            complete.TokenType.Should().Be(XmlTokenType.EndElement);
            complete.LocalNameEquals("a").Should().BeTrue();
            complete.BytesConsumed.Should().Be(afterA);
        }

        [TestMethod]
        [DataRow("", "\u0085", "\u2028", DisplayName = "XML 1.0 keeps NEL and LS")]
        [DataRow("<?xml version=\"1.1\"?>", "\n", "\n", DisplayName = "XML 1.1 turns NEL and LS into line feeds")]
        public void CDataAndComment_KeepEveryCharacter_AroundPlainRuns(string declaration, string nel, string ls)
        {
            var xml = declaration + "<r><![CDATA[plain run ] ]] ]>x <&\r\nnext\rline é中😀 a\u0085b\u2028c]]]><!--plain run - x -\r\nnext\rline é中😀 a\u0085b\u2028c--></r>";
            var reader = new Utf8XmlReader(System.Text.Encoding.UTF8.GetBytes(xml), new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow });
            while (reader.TokenType is not XmlTokenType.CData)
            {
                reader.Read().Should().BeTrue();
            }

            reader.GetString().Should().Be("plain run ] ]] ]>x <&\nnext\nline é中😀 a" + nel + "b" + ls + "c]");
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.Comment);
            reader.GetString().Should().Be("plain run - x -\nnext\nline é中😀 a" + nel + "b" + ls + "c");
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.Read().Should().BeFalse();
        }

        [TestMethod]
        [DataRow("<r><![CDATA[plain run\u0001]]></r>", DisplayName = "control character in CDATA")]
        [DataRow("<r><!--plain run\u0001--></r>", DisplayName = "control character in a comment")]
        [DataRow("<r><!--plain run--more--></r>", DisplayName = "two hyphens in a comment")]
        [DataRow("<r><![CDATA[plain run\uFFFE]]></r>", DisplayName = "noncharacter in CDATA")]
        [DataRow("<?xml version=\"1.1\"?><r><![CDATA[plain run\u0080]]></r>", DisplayName = "XML 1.1 C1 control in CDATA")]
        [DataRow("<?xml version=\"1.1\"?><r><!--plain run\u0080--></r>", DisplayName = "XML 1.1 C1 control in a comment")]
        public void CDataAndComment_RejectIllegalCharacters_AfterAPlainRun(string xml)
        {
            XmlReaderHarness.MustThrow(xml, new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow });
        }

        [TestMethod]
        public void CDataAndComment_ReportTheLine_AfterPlainRuns()
        {
            var line = (long?)null;
            try
            {
                var reader = new Utf8XmlReader("<r><![CDATA[one\ntwo\r\nthree]]><!--four\nfive-->\n<</r>"u8, new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow });
                while (reader.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                line = ex.LineNumber;
            }

            line.Should().Be(5);
        }

        [TestMethod]
        public void ProcessingInstruction_NormalizesLineEnds_AndKeepsAmpersands()
        {
            var reader = new Utf8XmlReader("<r><?p one\r\ntwo\rthree &amp; four?></r>"u8);
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.ProcessingInstruction);
            reader.GetString().Should().Be("one\ntwo\nthree &amp; four");
            reader.ValueIsEscaped.Should().BeTrue();

            var plain = new Utf8XmlReader("<r><?p one &amp; two?></r>"u8);
            plain.Read().Should().BeTrue();
            plain.Read().Should().BeTrue();
            plain.GetString().Should().Be("one &amp; two");
            plain.ValueIsEscaped.Should().BeFalse();
        }

        [TestMethod]
        public void UsualDeclaration_IsReadLikeAnyOther()
        {
            var usual = new Utf8XmlReader("<?xml version=\"1.0\" encoding=\"utf-8\"?><r/>"u8);
            usual.Read().Should().BeTrue();
            usual.TokenType.Should().Be(XmlTokenType.XmlDeclaration);
            usual.LocalNameEquals("xml").Should().BeTrue();
            usual.ValueTextEquals("version=\"1.0\" encoding=\"utf-8\"").Should().BeTrue();
            usual.Version.Should().Be(XmlVersion.Version10);
            usual.BytesConsumed.Should().Be(38);
            usual.Read().Should().BeTrue();
            usual.TokenType.Should().Be(XmlTokenType.StartElement);
            usual.LocalNameEquals("r").Should().BeTrue();

            var spaced = new Utf8XmlReader("<?xml version=\"1.0\"  encoding=\"utf-8\"?><r/>"u8);
            spaced.Read().Should().BeTrue();
            spaced.TokenType.Should().Be(XmlTokenType.XmlDeclaration);
            spaced.ValueTextEquals("version=\"1.0\"  encoding=\"utf-8\"").Should().BeTrue();

            var threw = false;
            try
            {
                var second = new Utf8XmlReader("<r/><?xml version=\"1.0\" encoding=\"utf-8\"?>"u8);
                while (second.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                threw = true;
                ex.Message.Should().Be("The XML declaration must be the first token in the document.");
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void DecodedAttributeValues_StayCorrect_AsTheirSharedBufferGrows()
        {
            var first = new string('a', 200) + "&amp;";
            var second = new string('b', 300) + "&lt;";
            var third = new string('c', 900) + "&gt;";
            var xml = System.Text.Encoding.UTF8.GetBytes("<e p=\"plain\" x=\"" + first + "\" y=\"" + second + "\" z=\"" + third + "\"/>");
            var reader = new Utf8XmlReader(xml);
            reader.Read().Should().BeTrue();
            reader.MoveToNextAttribute().Should().BeTrue();
            reader.ValueTextEquals("plain").Should().BeTrue();
            reader.MoveToNextAttribute().Should().BeTrue();
            reader.ValueTextEquals(new string('a', 200) + "&").Should().BeTrue();
            reader.MoveToNextAttribute().Should().BeTrue();
            reader.ValueTextEquals(new string('b', 300) + "<").Should().BeTrue();
            reader.MoveToNextAttribute().Should().BeTrue();
            reader.ValueTextEquals(new string('c', 900) + ">").Should().BeTrue();
            reader.MoveToNextAttribute().Should().BeFalse();
        }

        [TestMethod]
        public void Resume_EverySplitOffset_DrainsNamespacedDocument()
        {
            var xml = "<?xml version=\"1.0\"?><p:a xmlns:p=\"urn:x\" p:id=\"1\">hello</p:a>"u8.ToArray();
            for (var split = 1; split < xml.Length; split++)
            {
                DrainSplit(xml, split);
            }
        }

        [TestMethod]
        public void Resume_SplitInsideAttributeValue()
        {
            var first = "<e a=\"he"u8.ToArray();
            var second = "llo\"/>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            reader.Read().Should().BeFalse();
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.MoveToNextAttribute();
            resumed.GetString().Should().Be("hello");
        }

        [TestMethod]
        public void Resume_SplitInsideNamespacedStartTag()
        {
            var first = "<p:e xmlns:p=\"urn:x\""u8.ToArray();
            var second = " a=\"1\"><c/></p:e>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            reader.Read().Should().BeFalse();
            var combined = first[(int)reader.BytesConsumed..].ToArray().Concat(second).ToArray();
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.LocalNameEquals("e").Should().BeTrue();
            resumed.NamespaceUriEquals("urn:x").Should().BeTrue();
            while (resumed.Read())
            {
            }
        }

        [TestMethod]
        public void Resume_SplitInsidePrefix()
        {
            var first = "<p"u8.ToArray();
            var second = ":e xmlns:p=\"urn:x\"/>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            reader.Read().Should().BeFalse();
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.LocalNameEquals("e").Should().BeTrue();
            resumed.NamespaceUriEquals("urn:x").Should().BeTrue();
        }

        [TestMethod]
        public void Resume_SplitInsideTextThenMatchEndTag()
        {
            var first = "<a xmlns=\"urn:a\">he"u8.ToArray();
            var second = "llo</a>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeFalse();
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.TokenType.Should().Be(XmlTokenType.Text);
            resumed.GetString().Should().Be("hello");
            resumed.Read().Should().BeTrue();
            resumed.TokenType.Should().Be(XmlTokenType.EndElement);
            resumed.LocalNameEquals("a").Should().BeTrue();
            resumed.NamespaceUriEquals("urn:a").Should().BeTrue();
            resumed.Read().Should().BeFalse();
        }

        [TestMethod]
        public void Resume_SplitInsideXmlnsValue()
        {
            var first = "<p:e xmlns:p=\"ur"u8.ToArray();
            var second = "n:x\"/>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            reader.Read().Should().BeFalse();
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.NamespaceUriEquals("urn:x").Should().BeTrue();
        }

        [TestMethod]
        public void Resume_SplitOnEndTagKeepsNamespace()
        {
            var first = "<a xmlns=\"urn:a\">x</"u8.ToArray();
            var second = "a>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            while (reader.Read())
            {
            }

            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.TokenType.Should().Be(XmlTokenType.EndElement);
            resumed.LocalNameEquals("a").Should().BeTrue();
            resumed.NamespaceUriEquals("urn:a").Should().BeTrue();
        }

        [TestMethod]
        public void Resume_Xml11AfterDeclaration_KeepsVersion()
        {
            var first = "<?xml version=\"1.1\"?><e>a"u8.ToArray();
            var second = "b</e>"u8.ToArray();
            var reader = new Utf8XmlReader(first, false, default);
            while (reader.Read())
            {
            }

            reader.CurrentState.Snapshot!.Version.Should().Be(XmlVersion.Version11);
            var combined = ConcatRemainder(reader, first, second);
            var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
            resumed.Read().Should().BeTrue();
            resumed.Version.Should().Be(XmlVersion.Version11);
            while (resumed.Read())
            {
            }
        }

        #endregion

        #region Private Methods

        private static byte[] ConcatRemainder(Utf8XmlReader reader, byte[] first, byte[] second)
        {
            var consumed = (int)reader.BytesConsumed;
            return first[consumed..].Concat(second).ToArray();
        }

        private static void DrainSplit(byte[] xml, int split)
        {
            try
            {
                var first = xml[..split];
                var second = xml[split..];
                var reader = new Utf8XmlReader(first, false, default);
                while (reader.Read())
                {
                }

                var combined = ConcatRemainder(reader, first, second);
                var resumed = new Utf8XmlReader(combined, true, reader.CurrentState);
                while (resumed.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                throw new XmlException($"split {split}: {ex.Message}", ex);
            }
        }

        #endregion

    }

}
