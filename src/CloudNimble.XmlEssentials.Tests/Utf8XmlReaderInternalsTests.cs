using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Direct tests of every <see cref="Utf8XmlReader"/> internal method.
    /// </summary>
    [TestClass]
    public class Utf8XmlReaderInternalsTests
    {

        #region Public Methods

        [TestMethod]
        public void AdvanceAsciiTextRun_SkipsPrintableAndStopsOnHighByte()
        {
            var reader = new Utf8XmlReader("Widget0<"u8);
            reader.AdvanceAsciiTextRun(8).Should().BeTrue();
            reader.BytesConsumed.Should().Be(7);

            var ws = new Utf8XmlReader("   \t<"u8);
            ws.AdvanceAsciiTextRun(4).Should().BeFalse();
            ws.BytesConsumed.Should().Be(4);

            var lt = new Utf8XmlReader("<"u8);
            lt.AdvanceAsciiTextRun(1).Should().BeFalse();
            lt.BytesConsumed.Should().Be(0);
        }

        [TestMethod]
        public void AdvanceChar_NormalizesCrlfAndCountsLines()
        {
            var reader = new Utf8XmlReader("a\r\nb"u8);
            reader.AdvanceChar();
            reader.AdvanceChar();
            reader.Error("at-lf").LineNumber.Should().Be(2);
        }

        [TestMethod]
        public void ApplySnapshot_NullIsNoOp()
        {
            var reader = new Utf8XmlReader("<e/>"u8);
            reader.ApplySnapshot(null);
            reader.CurrentDepth.Should().Be(0);
            reader.TokenType.Should().Be(XmlTokenType.None);
        }

        [TestMethod]
        public void ApplySnapshot_RestoresDepth()
        {
            var reader = new Utf8XmlReader("<a><b/></a>"u8);
            reader.ReadStartElement();
            var snapshot = reader.CaptureSnapshot();
            snapshot.Depth.Should().Be(1);
            snapshot.SeenRoot.Should().BeTrue();

            var resumed = new Utf8XmlReader("<a><b/></a>"u8, true, new XmlReaderState(reader.CurrentState.Options, snapshot));
            resumed.CurrentDepth.Should().Be(1);
            resumed.CaptureSnapshot().SeenRoot.Should().BeTrue();
        }

        [TestMethod]
        public void CaptureSnapshot_CopiesDepthAndSeenRoot()
        {
            var reader = new Utf8XmlReader("<a><b/></a>"u8);
            reader.ReadStartElement();
            var snapshot = reader.CaptureSnapshot();
            snapshot.Depth.Should().Be(1);
            snapshot.ElementLocalNames.Should().Equal("a");
            snapshot.Finished.Should().BeFalse();
            snapshot.TokenType.Should().Be(XmlTokenType.StartElement);
            snapshot.Version.Should().Be(XmlVersion.Version10);
            snapshot.CurrentNamespaceUri.Should().BeEmpty();
            snapshot.XmlDeclarationAllowed.Should().BeFalse();
        }

        [TestMethod]
        public void CaptureSnapshot_CopiesNamespaceBindings()
        {
            var reader = new Utf8XmlReader("<p:a xmlns:p=\"urn:x\"></p:a>"u8);
            reader.ReadStartElement();
            var snapshot = reader.CaptureSnapshot();
            snapshot.ElementLocalNames.Should().Equal("a");
            snapshot.ElementNamespaceUris.Should().Equal("urn:x");
            snapshot.NamespacePrefixes.Should().Equal("p");
            snapshot.NamespaceUris.Should().Equal("urn:x");
            snapshot.CurrentNamespaceUri.Should().Be("urn:x");
        }

        [TestMethod]
        public void ContainsXml11LineEnding_DetectsNelAndLs()
        {
            Utf8XmlReader.ContainsXml11LineEnding([(byte)0xC2, 0x85]).Should().BeTrue();
            Utf8XmlReader.ContainsXml11LineEnding([(byte)0xE2, 0x80, 0xA8]).Should().BeTrue();
            Utf8XmlReader.ContainsXml11LineEnding("ab"u8).Should().BeFalse();
        }

        [TestMethod]
        public void CurrentAttribute_ReturnsSelectedAttribute()
        {
            var reader = new Utf8XmlReader("<e a=\"1\"/>"u8);
            reader.ReadStartElement();
            reader.MoveToNextAttribute().Should().BeTrue();
            var attribute = reader.CurrentAttribute();
            attribute.NameLength.Should().Be(1);
            attribute.ValueLength.Should().Be(1);
            attribute.PrefixLength.Should().Be(0);
        }

        [TestMethod]
        public void CurrentValueText_ReturnsDecodedText()
        {
            var reader = new Utf8XmlReader("<e>a&amp;b</e>"u8);
            reader.ReadStartElement();
            reader.ReadText();
            reader.CurrentValueText().Should().Be("a&b");
        }

        [TestMethod]
        public void DecodeAttributeValue_ExpandsEntities()
        {
            var reader = new Utf8XmlReader("<e a=\"x&amp;y\"/>"u8);
            reader.ReadStartElement();
            reader.MoveToNextAttribute();
            var attribute = reader.CurrentAttribute();
            var decoded = reader.DecodeAttributeValue(attribute.ValueStart, attribute.ValueLength, out var escaped, out var decodedLength);
            escaped.Should().BeTrue();
            Encoding.UTF8.GetString(decoded!.AsSpan(0, decodedLength)).Should().Be("x&y");
        }

        [TestMethod]
        public void DecodeAttributeValue_ReturnsNullWhenRaw()
        {
            var reader = new Utf8XmlReader("<e a=\"xy\"/>"u8);
            reader.ReadStartElement();
            reader.MoveToNextAttribute();
            var attribute = reader.CurrentAttribute();
            var decoded = reader.DecodeAttributeValue(attribute.ValueStart, attribute.ValueLength, out var escaped, out var decodedLength);
            decoded.Should().BeNull();
            escaped.Should().BeFalse();
            decodedLength.Should().Be(2);
        }

        [TestMethod]
        public void DecodeEntity_PredefinedAndNumeric()
        {
            Span<byte> buffer = stackalloc byte[8];
            var amp = new Utf8XmlReader("&amp;"u8);
            amp.DecodeEntity(buffer, out var ampWritten).Should().BeTrue();
            buffer[..ampWritten].ToArray().Should().Equal((byte)'&');

            var lt = new Utf8XmlReader("&lt;"u8);
            lt.DecodeEntity(buffer, out var ltWritten).Should().BeTrue();
            buffer[..ltWritten].ToArray().Should().Equal((byte)'<');

            var dec = new Utf8XmlReader("&#65;"u8);
            dec.DecodeEntity(buffer, out var decWritten).Should().BeTrue();
            Encoding.UTF8.GetString(buffer[..decWritten]).Should().Be("A");

            var hex = new Utf8XmlReader("&#x42;"u8);
            hex.DecodeEntity(buffer, out var hexWritten).Should().BeTrue();
            Encoding.UTF8.GetString(buffer[..hexWritten]).Should().Be("B");
        }

        [TestMethod]
        public void DecodeEntity_UndefinedThrows()
        {
            var reader = new Utf8XmlReader("&foo;"u8);
            var threw = false;
            try
            {
                _ = reader.DecodeEntity(stackalloc byte[8], out _);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void DecodeRune_AsciiAndMultibyte()
        {
            var ascii = new Utf8XmlReader("A"u8);
            var (cp, width) = ascii.DecodeRune(0);
            cp.Should().Be('A');
            width.Should().Be(1);

            var latin = new Utf8XmlReader("é"u8);
            var (latinCp, latinWidth) = latin.DecodeRune(0);
            latinCp.Should().Be('é');
            latinWidth.Should().Be(2);
        }

        [TestMethod]
        public void DecodeRune_InvalidUtf8Throws()
        {
            var reader = new Utf8XmlReader([(byte)0xFF]);
            var threw = false;
            try
            {
                _ = reader.DecodeRune(0);
            }
            catch (XmlException ex)
            {
                threw = true;
                ex.Message.Should().Contain("UTF-8");
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void DecodeValueSpan_ExpandsEntities()
        {
            var reader = new Utf8XmlReader("<e>a&amp;b</e>"u8);
            reader.ReadStartElement();
            var start = (int)reader.BytesConsumed;
            reader.DecodeValueSpan(start, "a&amp;b"u8.Length);
            reader.ValueIsEscaped.Should().BeTrue();
            reader.CurrentValueText().Should().Be("a&b");
        }

        [TestMethod]
        public void DecodeValueSpan_LeavesRawWhenNoEntity()
        {
            var reader = new Utf8XmlReader("<e>ab</e>"u8);
            reader.ReadStartElement();
            var start = (int)reader.BytesConsumed;
            reader.DecodeValueSpan(start, 2);
            reader.ValueIsEscaped.Should().BeFalse();
        }

        [TestMethod]
        public void Error_StoresBytePosition()
        {
            var reader = new Utf8XmlReader("<e/>"u8);
            reader.ReadStartElement();
            var ex = reader.Error("boom");
            ex.Message.Should().Be("boom");
            ex.BytePosition.Should().HaveValue();
            ex.LineNumber.Should().Be(1);
        }

        [TestMethod]
        public void HandleWhitespaceOnlyText_RespectsOptions()
        {
            var skipped = new Utf8XmlReader("<a> </a>"u8);
            skipped.ReadStartElement();
            skipped.HandleWhitespaceOnlyText().Should().BeFalse();

            var significant = new Utf8XmlReader("<a> </a>"u8, new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.Significant });
            significant.ReadStartElement();
            significant.HandleWhitespaceOnlyText().Should().BeTrue();
            significant.TokenType.Should().Be(XmlTokenType.Text);

            var none = new Utf8XmlReader("<a> </a>"u8, new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.None });
            none.ReadStartElement();
            var threw = false;
            try
            {
                none.HandleWhitespaceOnlyText();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void IsHexOrDec_AcceptsDigitsAndHexLetters()
        {
            Utf8XmlReader.IsHexOrDec((byte)'0', hex: false).Should().BeTrue();
            Utf8XmlReader.IsHexOrDec((byte)'9', hex: false).Should().BeTrue();
            Utf8XmlReader.IsHexOrDec((byte)'a', hex: false).Should().BeFalse();
            Utf8XmlReader.IsHexOrDec((byte)'a', hex: true).Should().BeTrue();
            Utf8XmlReader.IsHexOrDec((byte)'F', hex: true).Should().BeTrue();
            Utf8XmlReader.IsHexOrDec((byte)'g', hex: true).Should().BeFalse();
        }

        [TestMethod]
        public void NeedMore_FinalBlockThrows()
        {
            var reader = new Utf8XmlReader("<e/>"u8);
            var threw = false;
            try
            {
                _ = reader.NeedMore();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void NeedMore_NonFinalReturnsFalse()
        {
            var reader = new Utf8XmlReader("<e/>"u8, false, default);
            reader.NeedMore().Should().BeFalse();
        }

        [TestMethod]
        public void ParseInteger_ReadsCurrentText()
        {
            var reader = new Utf8XmlReader("<e>42</e>"u8);
            reader.ReadStartElement();
            reader.ReadText();
            reader.ParseInteger(int.Parse).Should().Be(42);

            var invalid = new Utf8XmlReader("<e>+1</e>"u8);
            invalid.ReadStartElement();
            invalid.ReadText();
            var threw = false;
            try
            {
                _ = invalid.ParseInteger(int.Parse);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void ParseXmlDeclaration_SetsVersionAndRejectsNonUtf8()
        {
            var version11 = new Utf8XmlReader("<?xml version=\"1.1\" encoding=\"utf-8\"?><e/>"u8);
            version11.ReadPiOrDeclaration();
            version11.Version.Should().Be(XmlVersion.Version11);

            var bad = new Utf8XmlReader("<?xml version=\"1.0\" encoding=\"latin1\"?><e/>"u8);
            var threw = false;
            try
            {
                _ = bad.ReadPiOrDeclaration();
            }
            catch (XmlException ex)
            {
                threw = true;
                ex.Message.Should().Contain("UTF-8");
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void ReadAttribute_StoresNameAndValue()
        {
            var reader = new Utf8XmlReader("a=\"1\"/>"u8);
            reader.ReadAttribute().Should().BeTrue();
            reader.AttributeCount.Should().Be(1);
        }

        [TestMethod]
        public void ReadCData_ReadsPayload()
        {
            var reader = new Utf8XmlReader("<e><![CDATA[x]]></e>"u8);
            reader.ReadStartElement();
            reader.ReadCData().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.CData);
            reader.CurrentValueText().Should().Be("x");
        }

        [TestMethod]
        public void ReadComment_ReadsPayload()
        {
            var reader = new Utf8XmlReader("<!--hi--><e/>"u8);
            reader.ReadComment().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.Comment);
            reader.GetComment().Should().Be("hi");
        }

        [TestMethod]
        public void ReadEndElement_MatchesStart()
        {
            var reader = new Utf8XmlReader("<e></e>"u8);
            reader.ReadStartElement();
            reader.ReadEndElement().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.CurrentDepth.Should().Be(0);
        }

        [TestMethod]
        public void ReadName_ReadsPrefixAndLocal()
        {
            var reader = new Utf8XmlReader("p:e "u8);
            reader.ReadName(out var prefixLength, out var nameLength).Should().BeTrue();
            prefixLength.Should().Be(1);
            nameLength.Should().Be(3);
        }

        [TestMethod]
        public void ReadName_RejectsInvalidStartAndMultipleColons()
        {
            var invalid = new Utf8XmlReader("1e "u8);
            invalid.ReadName(out _, out _).Should().BeFalse();

            var colons = new Utf8XmlReader("a:b:c "u8);
            var threw = false;
            try
            {
                _ = colons.ReadName(out _, out _);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void ReadPiOrDeclaration_ReadsXmlDeclaration()
        {
            var reader = new Utf8XmlReader("<?xml version=\"1.0\"?><e/>"u8);
            reader.ReadPiOrDeclaration().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.XmlDeclaration);
            reader.Version.Should().Be(XmlVersion.Version10);
        }

        [TestMethod]
        public void ReadStartElement_EmptyAndPaired()
        {
            var empty = new Utf8XmlReader("<e/>"u8);
            empty.ReadStartElement().Should().BeTrue();
            empty.IsEmptyElement.Should().BeTrue();
            empty.LocalNameEquals("e").Should().BeTrue();

            var paired = new Utf8XmlReader("<e></e>"u8);
            paired.ReadStartElement().Should().BeTrue();
            paired.IsEmptyElement.Should().BeFalse();
            paired.CurrentDepth.Should().Be(1);
        }

        [TestMethod]
        public void ReadText_ReadsCharacterData()
        {
            var reader = new Utf8XmlReader("<e>hi</e>"u8);
            reader.ReadStartElement();
            reader.ReadText().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.Text);
            reader.CurrentValueText().Should().Be("hi");
        }

        [TestMethod]
        public void SkipBomIfAtStart_ConsumesBom()
        {
            var xml = new byte[] { 0xEF, 0xBB, 0xBF, (byte)'<', (byte)'e', (byte)'/', (byte)'>' };
            var reader = new Utf8XmlReader(xml);
            reader.BytesConsumed.Should().Be(3);
            reader.SkipBomIfAtStart();
            reader.BytesConsumed.Should().Be(3);
        }

        [TestMethod]
        public void SkipWhitespaceBytes_AdvancesPastWhitespace()
        {
            var reader = new Utf8XmlReader(" \t\n<e/>"u8);
            reader.SkipWhitespaceBytes();
            reader.BytesConsumed.Should().Be(3);
        }

        [TestMethod]
        public void TrimAscii_TrimsXmlWhitespace()
        {
            Utf8XmlReader.TrimAscii("  hi\t").Should().Be("hi");
            Utf8XmlReader.TrimAscii("x").Should().Be("x");
        }

        [TestMethod]
        public void TryMatchUnprefixedEndName_AcceptsTerminator()
        {
            var reader = new Utf8XmlReader("<item>x</item>"u8);
            reader.ReadStartElement();
            reader.ReadText();
            reader.TryMatchUnprefixedEndName(out _, out _).Should().BeFalse();
            reader.ReadEndElement().Should().BeTrue();
            reader.LocalNameEquals("item").Should().BeTrue();

            var empty = new Utf8XmlReader("item>"u8);
            empty.TryMatchUnprefixedEndName(out _, out _).Should().BeFalse();
        }

        [TestMethod]
        public void ReadElementContent_CommentDisallowThrows()
        {
            var reader = new Utf8XmlReader("<a><!--c--></a>"u8, new XmlReaderOptions { CommentHandling = XmlCommentHandling.Disallow });
            reader.Read();
            var threw = false;
            try
            {
                reader.Read();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void ReadName_NonAsciiAndDoubleColon()
        {
            var badStart = new Utf8XmlReader("© "u8);
            badStart.ReadName(out _, out _).Should().BeFalse();

            var copy = new Utf8XmlReader("a© "u8);
            copy.ReadName(out _, out var n).Should().BeTrue();
            n.Should().Be(1);

            var continued = new Utf8XmlReader("aé "u8);
            continued.ReadName(out _, out var continuedLen).Should().BeTrue();
            continuedLen.Should().BeGreaterThan(1);

            var prefixed = new Utf8XmlReader("p:é "u8);
            prefixed.ReadName(out var pref, out var prefLen).Should().BeTrue();
            pref.Should().Be(1);
            prefLen.Should().BeGreaterThan(2);

            var colons = new Utf8XmlReader("a::b "u8);
            var threw = false;
            try
            {
                _ = colons.ReadName(out _, out _);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();

            var local = new Utf8XmlReader("p:© "u8);
            var localThrew = false;
            try
            {
                _ = local.ReadName(out _, out _);
            }
            catch (XmlException)
            {
                localThrew = true;
            }

            localThrew.Should().BeTrue();
        }

        [TestMethod]
        public void ReadText_FinalHighByteAndNonFinalCr()
        {
            var xml = "<e>hello"u8.ToArray().Concat("é"u8.ToArray()).ToArray();
            XmlReaderHarness.MustThrow(xml);

            var cr = new Utf8XmlReader("<e>a\r"u8, false, default);
            cr.Read().Should().BeTrue();
            cr.Read().Should().BeFalse();
        }

        [TestMethod]
        public void Read_DocumentLevelIncompleteEndTag_NeedMore()
        {
            var reader = new Utf8XmlReader("</e"u8, false, default);
            reader.Read().Should().BeFalse();
        }

        [TestMethod]
        public void TryParseDuration_ParsesDayTimeAndRejectsYearMonth()
        {
            Utf8XmlReader.TryParseDuration("PT1S", out var second).Should().BeTrue();
            second.Should().Be(TimeSpan.FromSeconds(1));
            Utf8XmlReader.TryParseDuration("P1D", out var day).Should().BeTrue();
            day.Should().Be(TimeSpan.FromDays(1));
            Utf8XmlReader.TryParseDuration("-PT1S", out var negative).Should().BeTrue();
            negative.Should().Be(-TimeSpan.FromSeconds(1));
            Utf8XmlReader.TryParseDuration("P1Y", out _).Should().BeFalse();
            Utf8XmlReader.TryParseDuration("P1M", out _).Should().BeFalse();
            Utf8XmlReader.TryParseDuration("PT", out _).Should().BeFalse();
            Utf8XmlReader.TryParseDuration("", out _).Should().BeFalse();
        }

        [TestMethod]
        public void TryParseInteger_RejectsPlusAndExponent()
        {
            var ok = new Utf8XmlReader("<e>7</e>"u8);
            ok.ReadStartElement();
            ok.ReadText();
            ok.TryParseInteger<int>(int.TryParse, out var n).Should().BeTrue();
            n.Should().Be(7);

            var plus = new Utf8XmlReader("<e>+7</e>"u8);
            plus.ReadStartElement();
            plus.ReadText();
            plus.TryParseInteger<int>(int.TryParse, out _).Should().BeFalse();

            var exponent = new Utf8XmlReader("<e>1e2</e>"u8);
            exponent.ReadStartElement();
            exponent.ReadText();
            exponent.TryParseInteger<int>(int.TryParse, out _).Should().BeFalse();
        }

        [TestMethod]
        public void TryReadMarkupDeclaration_CommentAndDtd()
        {
            var comment = new Utf8XmlReader("<!--c--><e/>"u8);
            comment.TryReadMarkupDeclaration().Should().BeTrue();
            comment.TokenType.Should().Be(XmlTokenType.Comment);

            var dtd = new Utf8XmlReader("<!DOCTYPE e><e/>"u8);
            var threw = false;
            try
            {
                _ = dtd.TryReadMarkupDeclaration();
            }
            catch (XmlException ex)
            {
                threw = true;
                ex.Message.Should().Contain("DTD");
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void ValidateChar_RejectsNulAndRestrictedLiteral()
        {
            var reader = new Utf8XmlReader("<?xml version=\"1.1\"?><e/>"u8);
            reader.ReadPiOrDeclaration();
            var nul = false;
            try
            {
                reader.ValidateChar(0, fromNcr: true);
            }
            catch (XmlException)
            {
                nul = true;
            }

            nul.Should().BeTrue();

            var restricted = false;
            try
            {
                reader.ValidateChar(1, fromNcr: false);
            }
            catch (XmlException)
            {
                restricted = true;
            }

            restricted.Should().BeTrue();

            reader.ValidateChar(1, fromNcr: true);
        }

        [TestMethod]
        public void InternUri_ReusesKnownAndCustom()
        {
            var reader = new Utf8XmlReader("<e/>"u8);
            var empty = reader.InternUri([]);
            empty.Should().BeSameAs(XmlUtf8.Empty);
            var xml = reader.InternUri(XmlUtf8.Xml);
            xml.Should().BeSameAs(XmlUtf8.Xml);
            var custom = reader.InternUri("urn:a"u8);
            reader.InternUri("urn:a"u8).Should().BeSameAs(custom);
            reader.LookupNamespace("xml"u8, out var xmlDeclared).IsEmpty.Should().BeFalse();
            xmlDeclared.Should().BeTrue();
            reader.LookupNamespace("xmlns"u8, out var xmlnsDeclared).SequenceEqual(XmlUtf8.Xmlns).Should().BeTrue();
            xmlnsDeclared.Should().BeTrue();
            reader.LookupNamespace("p"u8, out var pDeclared).IsEmpty.Should().BeTrue();
            pDeclared.Should().BeFalse();
            reader.LookupNamespace([], out var defaultDeclared).IsEmpty.Should().BeTrue();
            defaultDeclared.Should().BeTrue();
        }

        [TestMethod]
        public void LookupNamespace_FindsPushedBinding()
        {
            var reader = new Utf8XmlReader("<e/>"u8);
            reader.PushBinding(1, reader.InternPrefix("p"u8), reader.InternUri("urn:p"u8));
            reader.LookupNamespace("p"u8, out var declared).SequenceEqual("urn:p"u8).Should().BeTrue();
            declared.Should().BeTrue();
            reader.PopNamespaceScope(1);
            reader.LookupNamespace("p"u8, out var after).IsEmpty.Should().BeTrue();
            after.Should().BeFalse();
        }

        [TestMethod]
        public void ResolveElementNamespace_UndeclaredPrefixThrows()
        {
            var reader = new Utf8XmlReader("<e/>"u8);
            var threw = false;
            try
            {
                _ = reader.ResolveElementNamespace("p"u8);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
            reader.ResolveElementNamespace([]).IsEmpty.Should().BeTrue();
        }

        [TestMethod]
        public void Coverage_AttributeAndElementOverflow()
        {
            var xml = new StringBuilder("<e");
            for (var i = 0; i < 17; i++)
            {
                xml.Append(" a").Append(i).Append("=\"").Append(i).Append('"');
            }

            xml.Append("/>");
            var many = new Utf8XmlReader(Encoding.UTF8.GetBytes(xml.ToString()));
            many.ReadStartElement();
            many.AttributeCount.Should().Be(17);
            for (var i = 0; i < 9; i++)
            {
                many.MoveToNextAttribute().Should().BeTrue();
            }

            many.CurrentAttribute().NameLength.Should().Be(2);

            var nested = new StringBuilder();
            nested.Append("<n0 xmlns=\"urn:a\">");
            for (var i = 1; i < 17; i++)
            {
                nested.Append("<n").Append(i).Append('>');
            }

            nested.Append('x');
            for (var i = 16; i >= 0; i--)
            {
                nested.Append("</n").Append(i).Append('>');
            }

            var deep = new Utf8XmlReader(Encoding.UTF8.GetBytes(nested.ToString()));
            while (deep.Read() && deep.TokenType is not XmlTokenType.Text)
            {
            }

            deep.ElementLocalName(16).SequenceEqual("n16"u8).Should().BeTrue();
            deep.ElementNamespace(16).SequenceEqual("urn:a"u8).Should().BeTrue();
        }

        [TestMethod]
        public void Coverage_NamespaceOwnedAndOverflow()
        {
            var owned = new NamespaceBinding(1, "p"u8.ToArray(), "urn:p"u8.ToArray());
            owned.PrefixStart.Should().Be(-1);
            owned.UriStart.Should().Be(-1);

            var xml = new Utf8XmlReader("<xml:e xml:lang=\"en\"/>"u8);
            xml.Read();
            xml.NamespaceUriEquals(XmlNamespaces.Xml).Should().BeTrue();

            var deep = new StringBuilder("<xml:n0>");
            for (var i = 1; i < 17; i++)
            {
                deep.Append("<xml:n").Append(i).Append('>');
            }

            deep.Append('x');
            for (var i = 16; i >= 0; i--)
            {
                deep.Append("</xml:n").Append(i).Append('>');
            }

            var nested = new Utf8XmlReader(Encoding.UTF8.GetBytes(deep.ToString()));
            while (nested.Read() && nested.TokenType is not XmlTokenType.Text)
            {
            }

            nested.ElementNamespace(16).SequenceEqual(XmlUtf8.Xml).Should().BeTrue();

            var intern = new Utf8XmlReader("<e/>"u8);
            intern.SetCurrentNamespace("urn:not-in-buffer"u8);
            intern.CurrentNamespaceBytes().SequenceEqual("urn:not-in-buffer"u8).Should().BeTrue();
            intern.StoreElementNamespace(0, "urn:not-in-buffer"u8);
            intern.StoreElementNamespace(20, XmlUtf8.Xml);
            intern.StoreElementNamespace(8, XmlUtf8.Xml);
            intern.TryGetBufferOffset([], out _).Should().BeFalse();
            intern.ElementNamespace(8).SequenceEqual(XmlUtf8.Xml).Should().BeTrue();
            intern.ElementNamespace(20).SequenceEqual(XmlUtf8.Xml).Should().BeTrue();
            intern.PushBinding(1, "zz"u8, "urn:zz"u8);
            intern.LookupNamespace("zz"u8, out var zzDeclared).SequenceEqual("urn:zz"u8).Should().BeTrue();
            zzDeclared.Should().BeTrue();

            for (var i = 0; i < 17; i++)
            {
                intern.PushBinding(1, intern.InternPrefix(Encoding.UTF8.GetBytes("p" + i)), intern.InternUri("urn:p"u8));
            }

            intern.BindingAt(16).Depth.Should().Be(1);
            intern.LookupNamespace("p16"u8, out var declared).SequenceEqual("urn:p"u8).Should().BeTrue();
            declared.Should().BeTrue();
        }

        [TestMethod]
        public void Coverage_DecodeAndInternEdgePaths()
        {
            var text = new Utf8XmlReader("<e>a&amp;b</e>"u8);
            text.Read();
            text.Read();
            text.NamespaceUriSpan.IsEmpty.Should().BeTrue();

            var plus = new Utf8XmlReader("<e>+1</e>"u8);
            plus.Read();
            plus.Read();
            plus.TryGetByte(out _).Should().BeFalse();
            plus.TryGetInt16(out _).Should().BeFalse();
            plus.TryGetInt64(out _).Should().BeFalse();
            plus.TryGetSByte(out _).Should().BeFalse();
            plus.TryGetUInt16(out _).Should().BeFalse();
            plus.TryGetUInt32(out _).Should().BeFalse();
            plus.TryGetUInt64(out _).Should().BeFalse();

            var spaces = new Utf8XmlReader("<e>   </e>"u8, new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.Significant });
            spaces.Read();
            spaces.Read();
            spaces.TryGetBytesFromBase64(out var empty).Should().BeTrue();
            empty.Should().BeEmpty();

            var attr = new Utf8XmlReader("<e a=\"x&amp;y\"/>"u8);
            attr.ReadStartElement();
            attr.MoveToNextAttribute();
            Encoding.UTF8.GetString(attr.AttributeValue(attr.CurrentAttribute())).Should().Be("x&y");

            var incomplete = new Utf8XmlReader("<e a=\"&amp\"/>"u8, false, default);
            var decoded = incomplete.DecodeAttributeValue(6, 4, out _, out _);
            decoded.Should().BeNull();

            var entity = new Utf8XmlReader("&amp;"u8);
            var tiny = false;
            try
            {
                _ = entity.DecodeEntity(stackalloc byte[0], out _);
            }
            catch (XmlException)
            {
                tiny = true;
            }

            tiny.Should().BeTrue();

            Span<byte> utf8 = stackalloc byte[4];
            Utf8XmlReader.EncodeUtf8CodePoint(0xE9, utf8).Should().Be(2);
            Utf8XmlReader.EncodeUtf8CodePoint(0x20AC, utf8).Should().Be(3);
            Utf8XmlReader.EncodeUtf8CodePoint(0x1F600, utf8).Should().Be(4);

            var intern = new Utf8XmlReader("<e/>"u8);
            intern.InternUri("urn:1"u8);
            intern.InternUri("urn:2"u8);
            intern.InternUri("urn:3"u8);
            intern.InternUri("urn:4"u8);
            intern.InternUri("urn:5"u8).Should().NotBeNull();
            intern.PushBinding(1, intern.InternPrefix("a"u8), intern.InternUri("urn:a"u8));
            intern.PushBinding(1, intern.InternPrefix("b"u8), intern.InternUri("urn:b"u8));
            intern.PushBinding(1, intern.InternPrefix("c"u8), intern.InternUri("urn:c"u8));
            intern.PushBinding(1, intern.InternPrefix("d"u8), intern.InternUri("urn:d"u8));
            intern.PushBinding(1, intern.InternPrefix("e"u8), intern.InternUri("urn:e"u8));
            intern.PushBinding(1, intern.InternPrefix("f"u8), intern.InternUri("urn:f"u8));
            intern.PushBinding(1, intern.InternPrefix("g"u8), intern.InternUri("urn:g"u8));
            intern.PushBinding(1, intern.InternPrefix("h"u8), intern.InternUri("urn:h"u8));
            intern.PushBinding(1, intern.InternPrefix("i"u8), intern.InternUri("urn:i"u8));
            intern.LookupNamespace("i"u8, out var declaredI).SequenceEqual("urn:i"u8).Should().BeTrue();
            declaredI.Should().BeTrue();
            intern.CurrentElementNamespace().IsEmpty.Should().BeTrue();

            intern.EnsureDecodedCapacity(4);
            intern.EnsureDecodedCapacity(2);

            Utf8XmlReader.TrimXmlWhitespace("  x  "u8).SequenceEqual("x"u8).Should().BeTrue();
            Utf8XmlReader.TryParseCodePoint("G"u8, hex: true, out _).Should().BeFalse();

            var overflow = new Utf8XmlReader("<e>99999999999999999999</e>"u8);
            overflow.ReadStartElement();
            overflow.ReadText();
            var threw = false;
            try
            {
                _ = overflow.ParseInteger(int.Parse);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();

            var cut = new Utf8XmlReader("<e>a&amp"u8, false, default);
            cut.ReadStartElement();
            cut.DecodeValueSpan((int)cut.BytesConsumed, 5);
        }

        [TestMethod]
        public void XmlReaderState_InternalConstructorStoresSnapshot()
        {
            var reader = new Utf8XmlReader("<e/>"u8);
            reader.ReadStartElement();
            var snapshot = reader.CaptureSnapshot();
            var state = new XmlReaderState(new XmlReaderOptions { MaxDepth = 3 }, snapshot);
            state.Options.MaxDepth.Should().Be(3);
            state.Snapshot.Should().BeSameAs(snapshot);
        }

        #endregion

    }

}
