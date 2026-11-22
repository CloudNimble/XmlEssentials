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
