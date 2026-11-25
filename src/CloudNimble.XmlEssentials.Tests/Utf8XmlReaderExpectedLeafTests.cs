using System;
using System.Collections.Generic;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="Utf8XmlReader.TryReadExpectedLeaf"/> reads <c>&lt;name&gt;text&lt;/name&gt;</c> in one step for
    /// a caller that knows which element comes next. It must leave the reader exactly where three calls to
    /// <see cref="Utf8XmlReader.Read"/> leave it, and must read nothing when the input is any other shape.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderExpectedLeafTests
    {

        #region Public Methods

        [TestMethod]
        [DataRow("<r><n>42</n><m>x</m></r>", DisplayName = "the first child")]
        [DataRow("<r><n>4 2\t!</n></r>", DisplayName = "text with a blank and a tab inside it")]
        [DataRow("<r><n>a]]b\"'=;</n></r>", DisplayName = "punctuation that needs no escaping in text")]
        [DataRow("<r>\n  <n>42</n>\n  <m>x</m>\n</r>", DisplayName = "line feeds and blanks before the start tag")]
        [DataRow("<r>\r\n\t<n>42</n>\r\n</r>", DisplayName = "carriage returns and a tab before the start tag")]
        [DataRow("<r xmlns=\"urn:d\"><n>42</n><m>x</m></r>", DisplayName = "a default namespace in scope")]
        [DataRow("<p:r xmlns:p=\"urn:p\"><n>42</n><p:m>x</p:m></p:r>", DisplayName = "a prefix in scope that the child does not use")]
        [DataRow("<r xmlns=\"urn:d\"><e xmlns=\"\"><n>42</n></e><n>43</n></r>", DisplayName = "a default namespace that is taken away and comes back")]
        [DataRow("<r><e><n>42</n></e><n>43</n></r>", DisplayName = "the same name at two depths")]
        [DataRow("<r xml:space=\"preserve\"> <n>42</n> </r>", DisplayName = "white space that is preserved around the child")]
        [DataRow("<r>text<n>42</n>more</r>", DisplayName = "mixed content before and after the child")]
        public void ExpectedLeaf_LeavesTheReaderWhereThreeReadsDo(string xml)
        {
            foreach (var declaration in (string[])["", "<?xml version=\"1.0\"?>", "<?xml version=\"1.1\"?>"])
            {
                var utf8 = Encoding.UTF8.GetBytes(declaration + xml);
                var whole = Trace(utf8, useExpectedLeaf: true, out var taken);
                var plain = Trace(utf8, useExpectedLeaf: false, out _);
                taken.Should().BeGreaterThan(0, "the document holds a child the method reads");
                whole.Should().Equal(plain);
            }
        }

        [TestMethod]
        public void ExpectedLeaf_ReturnsTheTextAndStopsOnTheEndTag()
        {
            var reader = new Utf8XmlReader("<r a=\"1\">\n  <n>42</n><m/></r>"u8);
            reader.Read().Should().BeTrue();
            reader.MoveToNextAttribute().Should().BeTrue();
            reader.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out var content).Should().BeTrue();
            Encoding.UTF8.GetString(content).Should().Be("42");
            Describe(ref reader).Should().Be("EndElement n ns= depth=1 at=17 to=21 value= attributes=0 empty=False");
            reader.Read().Should().BeTrue();
            Describe(ref reader).Should().Be("StartElement m ns= depth=1 at=21 to=25 value= attributes=0 empty=True");
        }

        [TestMethod]
        [DataRow("<r><m>42</m></r>", DisplayName = "another name")]
        [DataRow("<r><nn>42</nn></r>", DisplayName = "a longer name that starts the same")]
        [DataRow("<r><n a=\"1\">42</n></r>", DisplayName = "an attribute")]
        [DataRow("<r><n >42</n></r>", DisplayName = "white space in the start tag")]
        [DataRow("<r><n/></r>", DisplayName = "an empty element")]
        [DataRow("<r><n></n></r>", DisplayName = "no text")]
        [DataRow("<r><n> 42</n></r>", DisplayName = "text that starts with a blank")]
        [DataRow("<r><n>\t42</n></r>", DisplayName = "text that starts with a tab")]
        [DataRow("<r><n>4&amp;2</n></r>", DisplayName = "a reference in the text")]
        [DataRow("<r><n>4>2</n></r>", DisplayName = "a greater-than sign in the text")]
        [DataRow("<r><n>4\n2</n></r>", DisplayName = "a line feed in the text")]
        [DataRow("<r><n>4\r2</n></r>", DisplayName = "a carriage return in the text")]
        [DataRow("<r><n>caf\u00E9</n></r>", DisplayName = "a character outside ASCII in the text")]
        [DataRow("<r><n>42<!--c--></n></r>", DisplayName = "a comment after the text")]
        [DataRow("<r><n>42<n>1</n></n></r>", DisplayName = "a child after the text")]
        [DataRow("<r><n><![CDATA[42]]></n></r>", DisplayName = "a CDATA section")]
        [DataRow("<r><n>42</n ></r>", DisplayName = "white space in the end tag")]
        [DataRow("<r><!--c--><n>42</n></r>", DisplayName = "a comment before the start tag")]
        [DataRow("<r>x<n>42</n></r>", DisplayName = "text before the start tag")]
        [DataRow("<r><p:n xmlns:p=\"urn:p\">42</p:n></r>", DisplayName = "a prefix")]
        public void ExpectedLeaf_ReadsNothing_WhenTheInputIsAnotherShape(string xml)
        {
            var utf8 = Encoding.UTF8.GetBytes(xml);
            var reader = new Utf8XmlReader(utf8);
            reader.Read().Should().BeTrue();
            var before = Describe(ref reader);
            reader.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out var content).Should().BeFalse();
            content.Length.Should().Be(0);
            Describe(ref reader).Should().Be(before);

            // The reader goes on as if it had not been asked.
            var rest = new List<string> { before };
            Drain(ref reader, rest);
            rest.Should().Equal(Tokens(utf8, useExpectedLeaf: false, out _));
        }

        [TestMethod]
        public void ExpectedLeaf_ReadsNothing_WhenALimitWouldBePassed()
        {
            var xml = "<r><n>4242</n></r>"u8;
            var tooDeep = new Utf8XmlReader(xml, new XmlReaderOptions { MaxDepth = 1 });
            tooDeep.Read().Should().BeTrue();
            tooDeep.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out _).Should().BeFalse();
            ThrownBy(ref tooDeep).Should().Be("MaxDepth exceeded (1). line 1 column 7");

            var longText = new Utf8XmlReader(xml, new XmlReaderOptions { MaxTextByteCount = 3 });
            longText.Read().Should().BeTrue();
            longText.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out _).Should().BeFalse();
            longText.Read().Should().BeTrue();
            ThrownBy(ref longText).Should().Be("MaxTextByteCount exceeded (3). line 1 column 11");

            var longName = new Utf8XmlReader("<r><name>42</name></r>"u8, new XmlReaderOptions { MaxNameByteCount = 3 });
            longName.Read().Should().BeTrue();
            longName.TryReadExpectedLeaf("<name>"u8, "</name>"u8, out _).Should().BeFalse();
            ThrownBy(ref longName).Should().Be("MaxNameByteCount exceeded (3). line 1 column 9");

            var atTheLimits = new Utf8XmlReader(xml, new XmlReaderOptions { MaxDepth = 2, MaxNameByteCount = 1, MaxTextByteCount = 4 });
            atTheLimits.Read().Should().BeTrue();
            atTheLimits.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out var content).Should().BeTrue();
            Encoding.UTF8.GetString(content).Should().Be("4242");
        }

        [TestMethod]
        public void ExpectedLeaf_AfterAnEmptyElementThatDeclaresANamespace_ReadsNothing()
        {
            // The declaration of the empty element is still in scope until the next read takes it away, and
            // only a read does that.
            var utf8 = "<r><e xmlns=\"urn:e\"/><n>42</n></r>"u8.ToArray();
            var reader = new Utf8XmlReader(utf8);
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out _).Should().BeFalse();
            reader.Read().Should().BeTrue();
            Describe(ref reader).Should().Be("StartElement n ns= depth=1 at=21 to=24 value= attributes=0 empty=False");
        }

        [TestMethod]
        public void ExpectedLeaf_OutsideAnElement_ReadsNothing()
        {
            // The document element is subject to checks an element inside it is not: that there is only one,
            // and that nothing but white space, comments and processing instructions is beside it.
            var first = new Utf8XmlReader("<n>42</n>"u8);
            first.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out _).Should().BeFalse();
            first.BytesConsumed.Should().Be(0);
            first.Read().Should().BeTrue();
            Describe(ref first).Should().Be("StartElement n ns= depth=0 at=0 to=3 value= attributes=0 empty=False");

            var second = new Utf8XmlReader("<n>42</n><n>43</n>"u8);
            second.Read().Should().BeTrue();
            second.Read().Should().BeTrue();
            second.Read().Should().BeTrue();
            Describe(ref second).Should().Be("EndElement n ns= depth=0 at=5 to=9 value= attributes=0 empty=False");
            second.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out _).Should().BeFalse();
            second.BytesConsumed.Should().Be(9);
            ThrownBy(ref second).Should().Be("Multiple root elements are not allowed. line 1 column 13");
        }

        [TestMethod]
        [DataRow("<r>\n  <n>42</n>\n  <m>7</m>\n</r>")]
        [DataRow("<?xml version=\"1.1\"?><r>\r\n<n>42</n></r>")]
        public void ExpectedLeaf_InABlockCutAtAnyByte_ReadsAllOfTheElementOrNoneOfIt(string xml)
        {
            var utf8 = Encoding.UTF8.GetBytes(xml);
            var rootEnd = xml.IndexOf("<r>", StringComparison.Ordinal) + 3;
            var leafEnd = xml.IndexOf("</n>", StringComparison.Ordinal) + 4;
            for (var length = rootEnd; length <= utf8.Length; length++)
            {
                var reader = new Utf8XmlReader(utf8.AsSpan(0, length), isFinalBlock: false, default);
                while (reader.TokenType is not XmlTokenType.StartElement)
                {
                    reader.Read().Should().BeTrue();
                }

                var consumed = reader.BytesConsumed;
                var read = reader.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out var content);
                read.Should().Be(length >= leafEnd, "the block is {0} bytes and the element ends at {1}", length, leafEnd);
                if (read)
                {
                    Encoding.UTF8.GetString(content).Should().Be("42");
                    reader.BytesConsumed.Should().Be(leafEnd);
                }
                else
                {
                    reader.BytesConsumed.Should().Be(consumed);
                }
            }
        }

        [TestMethod]
        public void ExpectedLeaf_AfterLineEnds_ReportsTheNextErrorOnTheRightLine()
        {
            var utf8 = "<r>\n  <n>42</n>\r\n  <m>7</m>\n  <n>43</n>\n</x>"u8.ToArray();
            var plain = new Utf8XmlReader(utf8);
            var expected = ThrownBy(ref plain);
            expected.Should().Be("End tag does not match start tag. line 5 column 5");

            var reader = new Utf8XmlReader(utf8);
            reader.Read().Should().BeTrue();
            reader.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out _).Should().BeTrue();
            reader.TryReadExpectedLeaf("<m>"u8, "</m>"u8, out _).Should().BeTrue();
            reader.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out _).Should().BeTrue();
            ThrownBy(ref reader).Should().Be(expected);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Describes what the reader reports about the token it is on.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>Every public property that describes the token, in one line.</returns>
        private static string Describe(ref Utf8XmlReader reader)
        {
            var name = Encoding.UTF8.GetString(reader.LocalNameSpan);
            if (!reader.PrefixSpan.IsEmpty)
            {
                name = Encoding.UTF8.GetString(reader.PrefixSpan) + ":" + name;
            }

            return $"{reader.TokenType} {name} ns={Encoding.UTF8.GetString(reader.NamespaceUriSpan)} depth={reader.CurrentDepth} at={reader.TokenStartIndex} to={reader.BytesConsumed} value={Encoding.UTF8.GetString(reader.ValueSpan)} attributes={reader.AttributeCount} empty={reader.IsEmptyElement}";
        }

        /// <summary>
        /// Reads to the end of the input, adding a line for each token.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="tokens">The lines so far.</param>
        private static void Drain(ref Utf8XmlReader reader, List<string> tokens)
        {
            while (reader.Read())
            {
                tokens.Add(Describe(ref reader));
            }
        }

        /// <summary>
        /// Reads until the reader throws.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The message of the exception.</returns>
        private static string ThrownBy(ref Utf8XmlReader reader)
        {
            try
            {
                while (reader.Read())
                {
                }
            }
            catch (XmlException ex)
            {
                return $"{ex.Message} line {ex.LineNumber} column {ex.LinePosition}";
            }

            return "nothing was thrown";
        }

        /// <summary>
        /// Reads a document and describes the reader after each token, leaving out the start tag and the text
        /// of every element named <c>n</c> that is a start tag, plain text and an end tag, which is what
        /// <see cref="Utf8XmlReader.TryReadExpectedLeaf"/> reads in one step.
        /// </summary>
        /// <param name="utf8">The document.</param>
        /// <param name="useExpectedLeaf">Whether such an element is read in one step, or with three reads.</param>
        /// <param name="taken">The number of elements that were read in one step.</param>
        /// <returns>One line for each token the two ways of reading both stop on.</returns>
        private static List<string> Trace(byte[] utf8, bool useExpectedLeaf, out int taken)
        {
            var tokens = Tokens(utf8, useExpectedLeaf, out taken);
            if (useExpectedLeaf)
            {
                return tokens;
            }

            // Take out what the one-step read does not stop on: white space before a start tag n with no
            // attributes, that start tag, and the text after it, when what follows is the end tag n.
            var kept = new List<string>();
            for (var i = 0; i < tokens.Count; i++)
            {
                var start = IsWhiteSpaceText(tokens[i]) ? i + 1 : i;
                if (start + 2 < tokens.Count
                    && tokens[start].StartsWith("StartElement n ", StringComparison.Ordinal)
                    && tokens[start].EndsWith(" attributes=0 empty=False", StringComparison.Ordinal)
                    && tokens[start + 1].StartsWith("Text ", StringComparison.Ordinal)
                    && tokens[start + 2].StartsWith("EndElement n ", StringComparison.Ordinal)
                    && IsPlainLeafText(tokens[start + 1]))
                {
                    i = start + 1;
                    continue;
                }

                kept.Add(tokens[i]);
            }

            return kept;
        }

        /// <summary>
        /// Reads a document and describes the reader after each token.
        /// </summary>
        /// <param name="utf8">The document.</param>
        /// <param name="useExpectedLeaf">Whether an element named <c>n</c> is read in one step when it can be.</param>
        /// <param name="taken">The number of elements that were read in one step.</param>
        /// <returns>One line for each token the reader stopped on.</returns>
        private static List<string> Tokens(byte[] utf8, bool useExpectedLeaf, out int taken)
        {
            taken = 0;
            var tokens = new List<string>();
            var reader = new Utf8XmlReader(utf8);
            var inElement = false;
            while (true)
            {
                if (useExpectedLeaf && inElement && reader.TryReadExpectedLeaf("<n>"u8, "</n>"u8, out _))
                {
                    taken++;
                    tokens.Add(Describe(ref reader));
                    continue;
                }

                if (!reader.Read())
                {
                    break;
                }

                inElement = reader.CurrentDepth > 0 || reader.TokenType is XmlTokenType.StartElement && !reader.IsEmptyElement;
                tokens.Add(Describe(ref reader));
            }

            return tokens;
        }

        /// <summary>
        /// Returns whether a described token is text made only of white space.
        /// </summary>
        /// <param name="token">The line that describes the token.</param>
        /// <returns><see langword="true"/> if the token is white space text.</returns>
        private static bool IsWhiteSpaceText(string token)
        {
            if (!token.StartsWith("Text ", StringComparison.Ordinal))
            {
                return false;
            }

            var start = token.IndexOf(" value=", StringComparison.Ordinal) + 7;
            var end = token.LastIndexOf(" attributes=", StringComparison.Ordinal);
            foreach (var c in token.AsSpan(start, end - start))
            {
                if (!char.IsWhiteSpace(c))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns whether a described text token is text the one-step read takes: it does not start with
        /// white space and holds nothing that is decoded or counted as a line end.
        /// </summary>
        /// <param name="token">The line that describes the text token.</param>
        /// <returns><see langword="true"/> if the text is plain.</returns>
        private static bool IsPlainLeafText(string token)
        {
            var start = token.IndexOf(" value=", StringComparison.Ordinal) + 7;
            var end = token.LastIndexOf(" attributes=", StringComparison.Ordinal);
            var value = token[start..end];
            return value.Length > 0 && value[0] > ' ' && value.AsSpan().IndexOfAny("<&>\r\n") < 0 && Ascii.IsValid(value);
        }

        #endregion

    }

}
