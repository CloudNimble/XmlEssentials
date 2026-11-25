using System;
using System.Linq;
using System.Text;
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
        [DataRow("<r><a>one</a><b x=\"1\">two</b><c></d></r>", "End tag does not match start tag. @36 line 1 column 37", DisplayName = "an error on the first line")]
        [DataRow("<r>\n  <a>one</a>\n  <b x=\"1\">two</b><c></d>\n</r>", "End tag does not match start tag. @42 line 3 column 26", DisplayName = "an error on the third line")]
        [DataRow("<r>\r\n  <a>one</a>\r\n  <b x=\"1\">t\r\nwo</b>\r\n<c></d>\r\n</r>", "End tag does not match start tag. @48 line 5 column 8", DisplayName = "an error after a line end inside text")]
        public void Resume_AtEverySplitOffset_ReportsAnErrorAtTheSamePlace(string xml, string single)
        {
            // The end tag does not match. The reader that meets it was resumed from a state, at some point
            // earlier in the document, and must give the line and the column a single pass gives.
            var utf8 = Encoding.UTF8.GetBytes(xml);
            var expected = PositionOfTheError(utf8, utf8.Length);
            expected.Should().Be(single);

            for (var split = 1; split < xml.IndexOf("</d>", StringComparison.Ordinal); split++)
            {
                PositionOfTheError(utf8, split).Should().Be(expected, "the first block ends at byte {0}", split);
            }
        }

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
        [DataRow(1, true)]
        [DataRow(9, false)]
        [DataRow(9, true)]
        public void TrySkip_OnAPartialBlock_KeepsTheAttributesOfTheStartTag(int attributeCount, bool valuesNeedDecoding)
        {
            // The child that is read on the way has as many attributes as the element, with other values. Nine
            // is more than the reader keeps inline, and a value with a reference is decoded into a buffer.
            var names = new[] { "a", "b", "c", "d", "e", "f", "g", "h", "i" }.Take(attributeCount).ToArray();
            var own = string.Concat(names.Select(n => " " + n + "=\"" + (valuesNeedDecoding ? "&lt;" : "own-") + n + "\""));
            var inner = string.Concat(names.Select(n => " " + n + "=\"" + (valuesNeedDecoding ? "&gt;" : "inner-") + n + n + "\""));
            var startTag = "<r><e" + own + ">";
            var element = startTag + "<f" + inner + ">x</f></e>";
            var whole = Encoding.UTF8.GetBytes(element + "<g" + inner + "/></r>");
            for (var split = startTag.Length; split < element.Length; split++)
            {
                var reader = new Utf8XmlReader(whole.AsSpan(0, split), false, default);
                reader.Read().Should().BeTrue();
                reader.Read().Should().BeTrue();

                reader.TrySkip().Should().BeFalse();
                reader.TokenType.Should().Be(XmlTokenType.StartElement);
                reader.LocalNameEquals("e").Should().BeTrue();
                reader.AttributeCount.Should().Be(attributeCount);
                foreach (var name in names)
                {
                    reader.MoveToNextAttribute().Should().BeTrue();
                    reader.LocalNameEquals(name).Should().BeTrue();
                    reader.GetString().Should().Be((valuesNeedDecoding ? "<" : "own-") + name, "the block ends at byte {0}", split);
                }

                reader.MoveToNextAttribute().Should().BeFalse();
            }

            // When all of the element is there, the reader goes on to the attributes of the next start tag.
            var complete = new Utf8XmlReader(whole, false, default);
            complete.Read().Should().BeTrue();
            complete.Read().Should().BeTrue();
            complete.TrySkip().Should().BeTrue();
            complete.Read().Should().BeTrue();
            complete.LocalNameEquals("g").Should().BeTrue();
            foreach (var name in names)
            {
                complete.MoveToNextAttribute().Should().BeTrue();
                complete.LocalNameEquals(name).Should().BeTrue();
                complete.GetString().Should().Be((valuesNeedDecoding ? ">" : "inner-") + name + name);
            }

            complete.MoveToNextAttribute().Should().BeFalse();
        }

        [TestMethod]
        public void TrySkip_OnAPartialBlock_BelowTheInlineDepth_KeepsTheOpenElements_AndTheReaderResumes()
        {
            // Ten open elements are more than the reader keeps inline, and the skip goes ten deeper.
            var outer = Enumerable.Range(0, 10).Select(i => "d" + i).ToArray();
            var inner = Enumerable.Range(0, 10).Select(i => "q:c" + i).ToArray();
            var element = "<p:e xmlns:q=\"urn:q\" a=\"1\">"
                + string.Concat(inner.Select(n => "<" + n + ">")) + "x" + string.Concat(inner.Reverse().Select(n => "</" + n + ">"))
                + "</p:e>";

            SkipFailsAtEverySplit_ThenTheReaderResumes(
                string.Concat(outer.Select(n => "<" + n + " xmlns:p=\"urn:" + n + "\">")),
                element,
                "<p:t/>" + string.Concat(outer.Reverse().Select(n => "</" + n + ">")),
                "StartElement p:e{urn:d9} open @xmlns:q{http://www.w3.org/2000/xmlns/}=[urn:q] @:a{}=[1]");
        }

        [TestMethod]
        public void TrySkip_OnAPartialBlock_KeepsTheNamespacesOfTheStartTag_AndTheReaderResumes()
        {
            // The element declares a prefix with a URI that needs decoding, uses it on an attribute, and sets
            // xml:space. Its child has nine attributes and a declaration of its own.
            const string element = "<e p:a=\"&lt;1\" xmlns:p=\"urn:a&amp;b\" xml:space=\"preserve\" b=\"2\" c=\"3\" d=\"4\" e=\"5\" f=\"6\" g=\"7\">"
                + "<p:f p:a=\"&gt;2\" xmlns:q=\"urn:c&amp;d\" h1=\"1\" h2=\"2\" h3=\"3\" h4=\"4\" h5=\"5\" h6=\"6\" h7=\"7\"> <q:g q:z=\"&amp;\"/> </p:f></e>";

            SkipFailsAtEverySplit_ThenTheReaderResumes(
                "<r>",
                element,
                "<t/></r>",
                "StartElement :e{} open @p:a{urn:a&b}=[<1] @xmlns:p{http://www.w3.org/2000/xmlns/}=[urn:a&b]"
                + " @xml:space{http://www.w3.org/XML/1998/namespace}=[preserve] @:b{}=[2] @:c{}=[3] @:d{}=[4] @:e{}=[5] @:f{}=[6] @:g{}=[7]");
        }

        [TestMethod]
        [DataRow("<r>aaaaa", "MaxTextByteCount exceeded (4).", DisplayName = "plain text")]
        [DataRow("<r>&amp;aaaa", "MaxTextByteCount exceeded (4).", DisplayName = "text that starts with a reference")]
        [DataRow("<r>a\raaa", "MaxTextByteCount exceeded (4).", DisplayName = "text with a line end to normalize")]
        [DataRow("<r><![CDATA[aaaaaaa", "MaxTextByteCount exceeded (4).", DisplayName = "CDATA")]
        [DataRow("<r><!--aaaaaaa", "MaxCommentByteCount exceeded (4).", DisplayName = "comment")]
        [DataRow("<r><!--aaaaa--", "MaxCommentByteCount exceeded (4).", DisplayName = "comment that may be ending")]
        [DataRow("<r><e a=\"aaaaa", "MaxTextByteCount exceeded (4).", DisplayName = "attribute value")]
        [DataRow("<r><e a=\"aa&am", "MaxTextByteCount exceeded (4).", DisplayName = "attribute value that ends inside a reference")]
        [DataRow("<r><?p aaaaaa", "MaxTextByteCount exceeded (4).", DisplayName = "processing instruction")]
        public void PartialBlock_WithMoreOfATokenThanItsLimitAllows_IsRefusedBeforeTheTokenEnds(string block, string message)
        {
            // The token has no end yet. What has arrived of it is already over the limit, so waiting for the
            // rest could only let a stream that never ends it go on being buffered.
            var options = new XmlReaderOptions { MaxCommentByteCount = 4, MaxTextByteCount = 4 };
            string? actual = null;
            try
            {
                var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(block), false, new XmlReaderState(options));
                reader.Read().Should().BeTrue();
                reader.Read();
            }
            catch (XmlException ex)
            {
                actual = ex.Message;
            }

            actual.Should().Be(message);
        }

        [TestMethod]
        [DataRow("<r>aaaa", "</r>", "Text :{} [aaaa]", DisplayName = "plain text")]
        [DataRow("<r><![CDATA[aaaa]]", "></r>", "CData :{} [aaaa]", DisplayName = "CDATA, cut inside its terminator")]
        [DataRow("<r><![CDATA[aaaaaa", "]]></r>", null, DisplayName = "CDATA whose last two bytes might be the terminator")]
        [DataRow("<r><!--aaaa--", "></r>", "Comment :{} [aaaa]", DisplayName = "comment, cut inside its terminator")]
        public void PartialBlock_WithNoMoreOfATokenThanItsLimitAllows_AsksForMore(string block, string rest, string? token)
        {
            var options = new XmlReaderOptions { MaxCommentByteCount = 4, MaxTextByteCount = 4 };
            var first = Encoding.UTF8.GetBytes(block);
            var reader = new Utf8XmlReader(first, false, new XmlReaderState(options));
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeFalse();
            reader.BytesConsumed.Should().Be(3);

            var tokens = new System.Collections.Generic.List<string>();
            string? failure = null;
            try
            {
                var resumed = new Utf8XmlReader(ConcatRemainder(reader, first, Encoding.UTF8.GetBytes(rest)), true, reader.CurrentState);
                XmlReaderHarness.AppendTokens(ref resumed, tokens);
            }
            catch (XmlException ex)
            {
                failure = ex.Message;
            }

            // The limit is not passed until the end shows how long the token is.
            if (token is null)
            {
                failure.Should().Be("MaxTextByteCount exceeded (4).");
            }
            else
            {
                failure.Should().BeNull();
                tokens.Should().Equal(token, "EndElement :r{}");
            }
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
                reader.CurrentDepth.Should().Be(1);
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
        public void TrySkip_OnAPartialBlock_ACopyTakenBeforeTheRead_SkipsTheElementWholeInTheNextBlock()
        {
            // The way to skip an element that may not be all in the block: keep a copy of the reader from before
            // the Read that reached the start tag, and go back to it when TrySkip returns false. The next block
            // then starts at the start tag again, and the element is skipped whole however the blocks fall.
            var whole = "<r><skip a=\"1\"><x>one</x><y/></skip><keep>k</keep></r>"u8.ToArray();
            for (var split = 1; split < whole.Length; split++)
            {
                var tokens = new System.Collections.Generic.List<string>();
                var state = default(XmlReaderState);
                var start = 0;
                foreach (var end in (int[])[split, whole.Length])
                {
                    var reader = new Utf8XmlReader(whole.AsSpan(start, end - start), end == whole.Length, state);
                    while (true)
                    {
                        var before = reader;
                        if (!reader.Read())
                        {
                            break;
                        }

                        if (reader.TokenType is XmlTokenType.StartElement && reader.LocalNameEquals("skip"))
                        {
                            if (!reader.TrySkip())
                            {
                                reader = before;
                                break;
                            }

                            tokens.Add("Skipped to " + reader.TokenType + " " + Encoding.UTF8.GetString(reader.LocalNameSpan));
                            continue;
                        }

                        tokens.Add(reader.TokenType is XmlTokenType.Text
                            ? "Text " + reader.GetString()
                            : reader.TokenType + " " + Encoding.UTF8.GetString(reader.LocalNameSpan));
                    }

                    state = reader.CurrentState;
                    start += (int)reader.BytesConsumed;
                }

                tokens.Should().Equal(
                    ["StartElement r", "Skipped to EndElement skip", "StartElement keep", "Text k", "EndElement keep", "EndElement r"],
                    "the split is at byte {0}", split);
                start.Should().Be(whole.Length);
            }
        }

        [TestMethod]
        [DataRow("", "\u0085", "\u2028", DisplayName = "XML 1.0 keeps NEL and LS")]
        [DataRow("<?xml version=\"1.1\"?>", "\n", "\n", DisplayName = "XML 1.1 turns NEL and LS into line feeds")]
        public void CDataAndComment_KeepEveryCharacter_AroundPlainRuns(string declaration, string nel, string ls)
        {
            var xml = declaration + "<r><![CDATA[plain run ] ]] ]>x <&\r\nnext\rline é中😀 a\u0085b\u2028c]]]><!--plain run - x -\r\nnext\rline é中😀 a\u0085b\u2028c--></r>";
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml), new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow });
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
        [DataRow("", DisplayName = "XML 1.0")]
        [DataRow("<?xml version=\"1.1\"?>", DisplayName = "XML 1.1")]
        public void ShortText_ReadsTheSame_OnEitherSideOfTheBulkScan(string declaration)
        {
            string[] texts =
            [
                "1",
                "7 bytes",
                "8 bytes.",
                "9 bytes..",
                "fifteen bytes..",
                "sixteen bytes...",
                "seventeen bytes..",
                "a text that is a good deal longer than the short scan",
                "a b\tc",
                "x\ny",
                "sixteen bytes...\nand a line",
                "a>b",
                "tail]]",
                " lead and trail ",
            ];
            var xml = declaration + "<r>\n  <a>" + string.Join("</a>\n  <a>", texts) + "</a>\n  <a>a&amp;b</a>\n  <a>line\r\nend</a>\n</r>";
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml), new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.Insignificant });
            var read = new System.Collections.Generic.List<string>();
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.Text)
                {
                    read.Add(reader.GetString()!);
                }
            }

            read.Should().Equal([.. texts, "a&b", "line\nend"]);
        }

        [TestMethod]
        public void ShortText_Xml11LineEnd_BecomesALineFeed()
        {
            var xml = "<?xml version=\"1.1\"?><r>ab" + (char)0x85 + "cd" + (char)0x2028 + "ef</r>";
            var reader = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml));
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.GetString().Should().Be("ab\ncd\nef");
        }

        [TestMethod]
        [DataRow("<r>a]]>b</r>", DisplayName = "CDATA end in short text")]
        [DataRow("<r>ab\u0001</r>", DisplayName = "control character in short text")]
        [DataRow("<r>a&b</r>", DisplayName = "bare ampersand in short text")]
        public void ShortText_RejectsWhatLongTextRejects(string xml)
        {
            XmlReaderHarness.MustThrow(xml);
        }

        [TestMethod]
        public void ShortText_CountsItsLineFeeds()
        {
            var line = (long?)null;
            try
            {
                var reader = new Utf8XmlReader("<r>\n  <a>x\ny</a>\n  <a>z</a>\n<</r>"u8);
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
        public void Resume_EverySplitOffset_ReadsTheNamespacedDocumentTheSame()
        {
            var xml = "<?xml version=\"1.0\"?><p:a xmlns:p=\"urn:x\" p:id=\"1\">hello</p:a>"u8.ToArray();
            var options = new XmlReaderOptions();
            for (var split = 1; split < xml.Length; split++)
            {
                XmlReaderHarness.TokensSplitAt(xml, split, options).Should().Equal(
                    [
                        "XmlDeclaration :xml{} [version=\"1.0\"]",
                        "StartElement p:a{urn:x} open @xmlns:p{http://www.w3.org/2000/xmlns/}=[urn:x] @p:id{urn:x}=[1]",
                        "Text :{} [hello]",
                        "EndElement p:a{urn:x}",
                    ],
                    "the tokens must not depend on a split at byte {0}",
                    split);
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

        /// <summary>
        /// Reads a document that is not well formed as two blocks, and describes the exception: its message,
        /// byte position, line and column.
        /// </summary>
        /// <param name="utf8">The document.</param>
        /// <param name="split">The length of the first block. The whole length reads it in one pass.</param>
        /// <returns>The description, or null when no exception was thrown.</returns>
        private static string? PositionOfTheError(byte[] utf8, int split)
        {
            try
            {
                var first = new Utf8XmlReader(utf8.AsSpan(0, split), split == utf8.Length, default);
                while (first.Read())
                {
                }

                var second = new Utf8XmlReader(utf8.AsSpan((int)first.BytesConsumed), true, first.CurrentState);
                while (second.Read())
                {
                }

                return null;
            }
            catch (XmlException ex)
            {
                return ex.Message + " @" + ex.BytePosition + " line " + ex.LineNumber + " column " + ex.LinePosition;
            }
        }

        /// <summary>
        /// Cuts the input at every byte inside <paramref name="element"/> after its start tag, and checks that
        /// <see cref="Utf8XmlReader.TrySkip"/> returns false with the reader on the start tag as it was read,
        /// and that a reader resumed from there reads the rest of the document as an unbroken read does.
        /// </summary>
        /// <param name="before">The XML before the element: start tags only.</param>
        /// <param name="element">The element to skip.</param>
        /// <param name="after">The rest of the document.</param>
        /// <param name="startTag">The harness line for the start tag of the element.</param>
        private static void SkipFailsAtEverySplit_ThenTheReaderResumes(string before, string element, string after, string startTag)
        {
            var whole = Encoding.UTF8.GetBytes(before + element + after);
            var unbroken = XmlReaderHarness.Tokens(whole, new XmlReaderOptions());
            var index = before.Count(c => c is '<');
            unbroken[index].Should().Be(startTag);
            var startTagEnd = before.Length + element.IndexOf('>') + 1;
            for (var split = startTagEnd; split < before.Length + element.Length; split++)
            {
                var reader = new Utf8XmlReader(whole.AsSpan(0, split), false, default);
                for (var i = 0; i <= index; i++)
                {
                    reader.Read().Should().BeTrue();
                }

                var depth = reader.CurrentDepth;
                reader.TrySkip().Should().BeFalse();
                reader.CurrentDepth.Should().Be(depth);
                reader.BytesConsumed.Should().Be(startTagEnd);
                XmlReaderHarness.Describe(ref reader).Should().Be(startTag, "the block ends at byte {0}", split);

                var rest = new System.Collections.Generic.List<string>();
                var resumed = new Utf8XmlReader(whole.AsSpan(startTagEnd), true, reader.CurrentState);
                XmlReaderHarness.AppendTokens(ref resumed, rest);
                rest.Should().Equal(unbroken.Skip(index + 1), "the block ends at byte {0}", split);
            }
        }

        #endregion

    }

}
