using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Documents of the size and shape a finance or ERP system exports: megabytes of entries, elements with
    /// hundreds or thousands of attributes, and values of megabytes, through the reader, the DOM, the writer
    /// and the serializer.
    /// </summary>
    /// <remarks>
    /// The expected tokens come from <see cref="LedgerDump"/>, which writes them from the same values it writes
    /// the document from, not from a reader.
    /// </remarks>
    [TestClass]
    public class XmlLargeDocumentTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };

        // The same entries with each element on a line of its own, as an export is usually laid out.
        private static readonly Lazy<LedgerDump> Indented = new(() => LedgerDump.Create(3_000, 40, 5, 12, indented: true));

        // 3,000 entries of 40 attributes with 5 line items of 12: several megabytes.
        private static readonly Lazy<LedgerDump> Ledger = new(() => LedgerDump.Create(3_000, 40, 5, 12));

        // Entries and line items at the default limit of 1,000 attributes on an element.
        private static readonly Lazy<LedgerDump> Wide = new(() => LedgerDump.Create(3, 1_000, 2, 1_000));

        #endregion

        #region Public Methods

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Document_ParsesALedgerDump_AndWritesBackTheSameContent(bool indented)
        {
            var dump = (indented ? Indented : Ledger).Value;
            using var document = XmlDocument.Parse(dump.Utf8);
            var written = Write(document, new XmlWriterOptions { OmitXmlDeclaration = true });

            ShouldBeTheSameTokens(XmlReaderHarness.Tokens(written, new XmlReaderOptions()), dump.Tokens);
            using var again = XmlDocument.Parse(written);
            Write(again, new XmlWriterOptions { OmitXmlDeclaration = true }).AsSpan().SequenceEqual(written).Should().BeTrue("a document that the writer wrote is written back byte for byte");
        }

        [TestMethod]
        public void Document_WithARaisedLimit_FindsEachOfFiveThousandAttributes()
        {
            const int count = 5_000;
            var dump = LedgerDump.Create(2, count, 1, count);
            using var document = XmlDocument.Parse(dump.Utf8, new XmlDocumentOptions { MaxAttributeCount = count });

            var entry = default(XmlElement);
            foreach (var child in document.RootElement.EnumerateChildren())
            {
                entry = child;
            }

            entry.LocalName.Should().Be("Entry");
            entry.AttributeCount.Should().Be(count);
            for (var k = 0; k < count; k++)
            {
                var expected = LedgerDump.EntryAttribute(1, k);
                entry.GetAttribute(expected.Local).GetString().Should().Be(expected.Value);
            }

            var written = Write(document, new XmlWriterOptions { MaxAttributeCount = count, OmitXmlDeclaration = true });
            ShouldBeTheSameTokens(XmlReaderHarness.Tokens(written, new XmlReaderOptions { MaxAttributeCount = count }), dump.Tokens);
        }

        [TestMethod]
        public void Reader_OnValuesOfMegabytes_ReturnsEveryCharacter()
        {
            // A reference every thousand characters, so both values are decoded, and are megabytes long.
            var attribute = string.Concat(Enumerable.Range(0, 1_000).Select(i => new string((char)('a' + (i % 26)), 999) + "&"));
            var text = string.Concat(Enumerable.Range(0, 8_000).Select(i => new string((char)('A' + (i % 26)), 998) + "<\n"));
            var xml = Encoding.UTF8.GetBytes("<r a=\"" + attribute.Replace("&", "&amp;") + "\">" + text.Replace("<", "&lt;") + "</r>");
            xml.Length.Should().Be(1_000 * 1_004 + 8_000 * 1_003 + 12);

            var reader = new Utf8XmlReader(xml);
            reader.Read().Should().BeTrue();
            reader.MoveToNextAttribute().Should().BeTrue();
            string.Equals(reader.GetString(), attribute, StringComparison.Ordinal).Should().BeTrue();
            reader.MoveToElement();
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.Text);
            string.Equals(reader.GetString(), text, StringComparison.Ordinal).Should().BeTrue();
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            reader.Read().Should().BeFalse();

            // The same values when the input arrives a megabyte at a time.
            var blocks = XmlReaderHarness.TokensChunked(xml, 1_048_576, new XmlReaderOptions());
            ShouldBeTheSameTokens(blocks, ["StartElement :r{} open @:a{}=[" + attribute + "]", "Text :{} [" + text + "]", "EndElement :r{}"]);
        }

        [TestMethod]
        [DataRow(false, 4_096)]
        [DataRow(false, 65_536)]
        [DataRow(false, 1_000_003)]
        [DataRow(true, 4_096)]
        [DataRow(true, 65_536)]
        [DataRow(true, 1_000_003)]
        public void Reader_ReadsALedgerDump_TheSameInBlocksOfAnySize(bool indented, int blockSize)
        {
            var dump = (indented ? Indented : Ledger).Value;
            dump.Utf8.Length.Should().BeGreaterThan(4_000_000);

            ShouldBeTheSameTokens(XmlReaderHarness.TokensChunked(dump.Utf8, blockSize, new XmlReaderOptions()), dump.Tokens);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Reader_ReadsALedgerDump_InOnePass(bool indented)
        {
            var dump = (indented ? Indented : Ledger).Value;

            ShouldBeTheSameTokens(XmlReaderHarness.Tokens(dump.Utf8, new XmlReaderOptions()), dump.Tokens);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(4_096)]
        public void Reader_SkippingTheLayout_ReadsOnlyTheDataOfAnIndentedDump(int blockSize)
        {
            var dump = Indented.Value;
            var options = new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.Insignificant };
            var tokens = blockSize == 0 ? XmlReaderHarness.Tokens(dump.Utf8, options) : XmlReaderHarness.TokensChunked(dump.Utf8, blockSize, options);

            dump.Tokens.Count.Should().BeGreaterThan(dump.DataTokens.Count);
            ShouldBeTheSameTokens(tokens, dump.DataTokens);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(509)]
        [DataRow(8_192)]
        public void Reader_ReadsElementsAtTheAttributeLimit(int blockSize)
        {
            var dump = Wide.Value;
            var options = new XmlReaderOptions();
            var tokens = blockSize == 0 ? XmlReaderHarness.Tokens(dump.Utf8, options) : XmlReaderHarness.TokensChunked(dump.Utf8, blockSize, options);

            ShouldBeTheSameTokens(tokens, dump.Tokens);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(8_192)]
        public void Reader_WithARaisedLimit_ReadsElementsOfFiveThousandAttributes(int blockSize)
        {
            const int count = 5_000;
            var dump = LedgerDump.Create(2, count, 1, count);
            var options = new XmlReaderOptions { MaxAttributeCount = count };
            var tokens = blockSize == 0 ? XmlReaderHarness.Tokens(dump.Utf8, options) : XmlReaderHarness.TokensChunked(dump.Utf8, blockSize, options);

            ShouldBeTheSameTokens(tokens, dump.Tokens);
            FluentActions.Invoking(() => XmlReaderHarness.Tokens(dump.Utf8, new XmlReaderOptions()))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("MaxAttributeCount exceeded (1000).");
        }

        [TestMethod]
        public async Task Serializer_StreamsFiftyThousandRecords_AndWritesThemBack()
        {
            const int count = 50_000;
            var xml = new StringBuilder("<ArrayOfReadAheadItem>");
            for (var i = 0; i < count; i++)
            {
                xml.Append(Record(i));
            }

            var utf8 = Encoding.UTF8.GetBytes(xml.Append("</ArrayOfReadAheadItem>").ToString());
            utf8.Length.Should().BeGreaterThan(9_000_000);

            var read = new List<ReadAheadItem>(count);
            await foreach (var item in XmlSerializer.DeserializeAsyncEnumerable<ReadAheadItem>(new TrickleStream(utf8, 8_192), Bare))
            {
                XmlSerializer.Serialize(item, Bare).Should().Be(Record(read.Count));
                read.Add(item!);
            }

            read.Count.Should().Be(count);
            using var written = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(written, Values(read), Bare);
            written.ToArray().AsSpan().SequenceEqual(utf8).Should().BeTrue();
        }

        [TestMethod]
        public void TrySkip_OnAPartialBlock_OfAnElementAtTheAttributeLimit_KeepsItsAttributes_AndTheReaderResumes()
        {
            // The entry has a thousand attributes and so does each of its line items. The block is cut at
            // places all through the entry: inside the line items' start tags, and after them.
            var dump = Wide.Value;
            var index = dump.EntryTokenIndexes[1];
            var startTagEnd = dump.EntryStartTagEnds[1];
            var cuts = 0;
            for (var split = startTagEnd; split < dump.EntryEnds[1]; split += 257)
            {
                var reader = new Utf8XmlReader(dump.Utf8.AsSpan(0, split), false, default);
                for (var i = 0; i <= index; i++)
                {
                    reader.Read().Should().BeTrue();
                }

                reader.TrySkip().Should().BeFalse();
                reader.BytesConsumed.Should().Be(startTagEnd);
                XmlReaderHarness.Describe(ref reader).Should().Be(dump.Tokens[index], "the block ends at byte {0}", split);

                var rest = new List<string>();
                var resumed = new Utf8XmlReader(dump.Utf8.AsSpan(startTagEnd), true, reader.CurrentState);
                XmlReaderHarness.AppendTokens(ref resumed, rest);
                ShouldBeTheSameTokens(rest, dump.Tokens.GetRange(index + 1, dump.Tokens.Count - index - 1));
                cuts++;
            }

            cuts.Should().BeGreaterThan(100);
        }

        [TestMethod]
        [DataRow(1_000)]
        [DataRow(5_000)]
        public void Writer_WritesAnElementWithAsManyAttributesAsTheLimitAllows(int count)
        {
            // Attribute 2 of an entry has a quote, a line feed, markup and characters outside ASCII in its value.
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { MaxAttributeCount = count, OmitXmlDeclaration = true });
            var expected = new StringBuilder("StartElement :Entry{} empty");
            writer.WriteStartElement("Entry");
            for (var k = 2; k < count + 2; k++)
            {
                var attribute = LedgerDump.EntryAttribute(7, k);
                writer.WriteAttributeString(attribute.Local, attribute.Value);
                expected.Append(" @:").Append(attribute.Local).Append("{}=[").Append(attribute.Value).Append(']');
            }

            writer.WriteEndElement();
            writer.Dispose();

            ShouldBeTheSameTokens(XmlReaderHarness.Tokens(buffer.WrittenSpan, new XmlReaderOptions { MaxAttributeCount = count }), [expected.ToString()]);
        }

        #endregion

        #region Private Methods

        private static string Record(int index)
        {
            var i = index.ToString(CultureInfo.InvariantCulture);
            return "<ReadAheadItem A=\"x&amp;y" + i + "\" A1=\"" + i + "\" A2=\"2\" A3=\"3\" A4=\"4\" A5=\"5\" A6=\"6\" A7=\"7\" A8=\"8\">"
                + "<C B=\"1&amp;2 " + i + "\" B1=\"b" + i + "\" B2=\"b2\" B3=\"b3\" B4=\"b4\" B5=\"b5\" B6=\"b6\" B7=\"b7\" B8=\"b8\"/></ReadAheadItem>";
        }

        private static void ShouldBeTheSameTokens(List<string> actual, List<string> expected)
        {
            // One comparison a token, so a difference in a list of a hundred thousand names the token it is in.
            for (var i = 0; i < Math.Min(actual.Count, expected.Count); i++)
            {
                if (!string.Equals(actual[i], expected[i], StringComparison.Ordinal))
                {
                    actual[i].Should().Be(expected[i], "token {0} must be read as written", i);
                }
            }

            actual.Count.Should().Be(expected.Count);
        }

        private static async IAsyncEnumerable<T> Values<T>(IEnumerable<T> values)
        {
            foreach (var value in values)
            {
                yield return value;
            }

            await Task.CompletedTask;
        }

        private static byte[] Write(XmlDocument document, XmlWriterOptions options)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                document.WriteTo(writer);
            }

            return buffer.WrittenSpan.ToArray();
        }

        #endregion

    }

}
