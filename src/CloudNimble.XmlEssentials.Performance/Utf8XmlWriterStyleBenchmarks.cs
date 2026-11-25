using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Measures the parts of <see cref="Utf8XmlWriter"/> that the catalog benchmarks do not reach: indentation, the
    /// line ending that is written inside content, attributes on lines of their own, raw values, values written
    /// in segments, and names that were encoded ahead of time.
    /// </summary>
    /// <remarks>
    /// Each category has the BCL <see cref="XmlWriter"/> as its baseline where it has a counterpart, and
    /// <see cref="Utf8JsonWriter"/> beside it where System.Text.Json has the same feature. XmlEssentials reuses one
    /// writer for each set of options with <see cref="Utf8XmlWriter.Reset()"/>; the BCL writer has no reset.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class Utf8XmlWriterStyleBenchmarks
    {

        #region Fields

        private const int Count = 200;
        private static readonly JsonEncodedText JsonId = JsonEncodedText.Encode("id");
        private static readonly JsonEncodedText JsonName = JsonEncodedText.Encode("name");
        private static readonly JsonEncodedText JsonPrice = JsonEncodedText.Encode("price");
        private static readonly JsonEncodedText JsonQty = JsonEncodedText.Encode("qty");
        private static readonly XmlEncodedText XmlCatalog = XmlEncodedText.Encode("catalog");
        private static readonly XmlEncodedText XmlItem = XmlEncodedText.Encode("item");
        private static readonly XmlEncodedText XmlName = XmlEncodedText.Encode("name");
        private static readonly XmlEncodedText XmlPrice = XmlEncodedText.Encode("price");
        private static readonly XmlEncodedText XmlQty = XmlEncodedText.Encode("qty");
        private byte[] _binary = null!;
        private char[] _chars = null!;
        private ArrayBufferWriter<byte> _buffer = null!;
        private XmlWriterSettings _bclAttributesPerLine = null!;
        private XmlWriterSettings _bclCrLf = null!;
        private XmlWriterSettings _bclIndented = null!;
        private XmlWriterSettings _bclPlain = null!;
        private Utf8XmlWriter _crLf10 = null!;
        private Utf8XmlWriter _crLf11 = null!;
        private Utf8XmlWriter _indented = null!;
        private ArrayBufferWriter<byte> _jsonBuffer = null!;
        private Utf8JsonWriter _jsonIndented = null!;
        private Utf8JsonWriter _jsonPlain = null!;
        private byte[] _jsonRaw = null!;
        private Utf8XmlWriter _lf = null!;
        private string _lines = null!;
        private Utf8XmlWriter _perLine = null!;
        private Utf8XmlWriter _plain = null!;
        private byte[] _rawXml = null!;
        private string _rawXmlText = null!;
        private MemoryStream _stream = null!;
        private byte[] _utf8Text = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// BCL catalog with every attribute on a line of its own (<see cref="XmlWriterSettings.NewLineOnAttributes"/>).
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("AttributesPerLine")]
        public long BclXmlWriter_AttributesPerLine()
        {
            return WriteBcl(_bclAttributesPerLine, WriteBclWideCatalog);
        }

        /// <summary>
        /// BCL value written as base64 in blocks of 3,000 bytes.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Base64Segments")]
        public long BclXmlWriter_Base64Segments()
        {
            return WriteBcl(_bclPlain, writer =>
            {
                writer.WriteStartElement("data");
                for (var offset = 0; offset < _binary.Length; offset += 3_000)
                {
                    writer.WriteBase64(_binary, offset, Math.Min(3_000, _binary.Length - offset));
                }

                writer.WriteEndElement();
            });
        }

        /// <summary>
        /// BCL catalog of 200 items, the baseline for names encoded ahead of time. The BCL has no such names.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("EncodedNames")]
        public long BclXmlWriter_EncodedNames()
        {
            return WriteBcl(_bclPlain, WriteBclCatalog);
        }

        /// <summary>
        /// BCL indented catalog of 200 items, with a comment before each item.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("IndentedWrite")]
        public long BclXmlWriter_IndentedWrite()
        {
            return WriteBcl(_bclIndented, WriteBclCommentedCatalog);
        }

        /// <summary>
        /// BCL text that holds line breaks, each rewritten as CR LF (<see cref="NewLineHandling.Replace"/>).
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("NewLineText")]
        public long BclXmlWriter_NewLineText()
        {
            return WriteBcl(_bclCrLf, writer =>
            {
                writer.WriteStartElement("notes");
                for (var i = 0; i < Count; i++)
                {
                    writer.WriteElementString("note", _lines);
                }

                writer.WriteEndElement();
            });
        }

        /// <summary>
        /// BCL <see cref="XmlWriter.WriteRaw(string)"/>, which never checks what it is given.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("RawValue")]
        public long BclXmlWriter_RawValue()
        {
            return WriteBcl(_bclPlain, writer =>
            {
                writer.WriteStartElement("root");
                writer.WriteRaw(_rawXmlText);
                writer.WriteEndElement();
            });
        }

        /// <summary>
        /// BCL text written in blocks of 1,000 characters.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("StringSegments")]
        public long BclXmlWriter_StringSegments()
        {
            return WriteBcl(_bclPlain, writer =>
            {
                var text = _chars;
                writer.WriteStartElement("text");
                for (var round = 0; round < 20; round++)
                {
                    for (var offset = 0; offset < text.Length; offset += 1_000)
                    {
                        writer.WriteChars(_chars, offset, Math.Min(1_000, text.Length - offset));
                    }
                }

                writer.WriteEndElement();
            });
        }

        /// <summary>
        /// Returns pooled writer buffers.
        /// </summary>
        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _crLf10.Dispose();
            _crLf11.Dispose();
            _indented.Dispose();
            _jsonIndented.Dispose();
            _jsonPlain.Dispose();
            _lf.Dispose();
            _perLine.Dispose();
            _plain.Dispose();
            _stream.Dispose();
        }

        /// <summary>
        /// Builds the reused destinations, one writer for each set of options, and the values that are written.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            _buffer = new ArrayBufferWriter<byte>(262_144);
            _jsonBuffer = new ArrayBufferWriter<byte>(262_144);
            _stream = new MemoryStream(262_144);
            _bclPlain = new XmlWriterSettings { CloseOutput = false, Encoding = utf8, Indent = false, OmitXmlDeclaration = true };
            _bclIndented = new XmlWriterSettings { CloseOutput = false, Encoding = utf8, Indent = true, NewLineChars = "\n", OmitXmlDeclaration = true };
            _bclAttributesPerLine = new XmlWriterSettings { CloseOutput = false, Encoding = utf8, Indent = true, NewLineChars = "\n", NewLineOnAttributes = true, OmitXmlDeclaration = true };
            _bclCrLf = new XmlWriterSettings { CloseOutput = false, Encoding = utf8, Indent = false, NewLineChars = "\r\n", NewLineHandling = NewLineHandling.Replace, OmitXmlDeclaration = true };

            _plain = new Utf8XmlWriter(_buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            _indented = new Utf8XmlWriter(_buffer, new XmlWriterOptions { Indented = true, NewLine = "\n", OmitXmlDeclaration = true });
            _perLine = new Utf8XmlWriter(_buffer, new XmlWriterOptions { AttributeStyle = XmlAttributeStyle.PerLine, Indented = true, NewLine = "\n", OmitXmlDeclaration = true });
            _lf = new Utf8XmlWriter(_buffer, new XmlWriterOptions { NewLine = "\n", OmitXmlDeclaration = true });
            _crLf10 = new Utf8XmlWriter(_buffer, new XmlWriterOptions { NewLine = "\r\n", OmitXmlDeclaration = true });
            _crLf11 = new Utf8XmlWriter(_buffer, new XmlWriterOptions { NewLine = "\r\n", OmitXmlDeclaration = true, Version = XmlVersion.Version11 });
            _jsonPlain = new Utf8JsonWriter(_jsonBuffer);
            _jsonIndented = new Utf8JsonWriter(_jsonBuffer, new JsonWriterOptions { Indented = true, NewLine = "\n" });

            // Fifty lines of text, the shape of a long note: most of it is copied as it is, and each
            // line break is the one character the writer has to look at.
            _lines = string.Join("\n", Enumerable.Range(0, 50).Select(i => $"Line {i} of a note that is long enough to be copied as a run of plain text."));
            _chars = _lines.ToCharArray();
            _utf8Text = Encoding.UTF8.GetBytes(_lines);
            _binary = new byte[60_000];
            new Random(11).NextBytes(_binary);

            // A fragment of 200 items, as a caller would hold it after writing it with another writer.
            _plain.Reset();
            _buffer.ResetWrittenCount();
            WriteEssentialsCatalog(_plain);
            _plain.Flush();
            _rawXml = _buffer.WrittenSpan.ToArray();
            _rawXmlText = Encoding.UTF8.GetString(_rawXml);
            _jsonRaw = Encoding.UTF8.GetBytes("[" + string.Join(",", Enumerable.Range(0, Count).Select(i => $"{{\"id\":{i},\"name\":\"Widget{i}\",\"qty\":{i},\"price\":9.99}}")) + "]");
        }

        /// <summary>
        /// System.Text.Json value written as base64 in segments of 3,000 bytes.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("Base64Segments")]
        public long Utf8JsonWriter_Base64Segments()
        {
            ResetJson(_jsonPlain);
            for (var offset = 0; offset < _binary.Length; offset += 3_000)
            {
                var length = Math.Min(3_000, _binary.Length - offset);
                _jsonPlain.WriteBase64StringSegment(_binary.AsSpan(offset, length), offset + length == _binary.Length);
            }

            _jsonPlain.Flush();
            return _jsonBuffer.WrittenCount;
        }

        /// <summary>
        /// System.Text.Json catalog of 200 items with property names as <see cref="JsonEncodedText"/>.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("EncodedNames")]
        public long Utf8JsonWriter_EncodedNames()
        {
            ResetJson(_jsonPlain);
            WriteJsonCatalog(_jsonPlain, commented: false);
            _jsonPlain.Flush();
            return _jsonBuffer.WrittenCount;
        }

        /// <summary>
        /// System.Text.Json indented catalog of 200 items, with a comment before each item, as the XML writers write one.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("IndentedWrite")]
        public long Utf8JsonWriter_IndentedWrite()
        {
            ResetJson(_jsonIndented);
            WriteJsonCatalog(_jsonIndented, commented: true);
            _jsonIndented.Flush();
            return _jsonBuffer.WrittenCount;
        }

        /// <summary>
        /// System.Text.Json raw value, checked before it is written.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("RawValue")]
        public long Utf8JsonWriter_RawValue()
        {
            ResetJson(_jsonPlain);
            _jsonPlain.WriteRawValue(_jsonRaw);
            _jsonPlain.Flush();
            return _jsonBuffer.WrittenCount;
        }

        /// <summary>
        /// System.Text.Json raw value, written without being checked.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("RawValue")]
        public long Utf8JsonWriter_RawValue_SkipValidation()
        {
            ResetJson(_jsonPlain);
            _jsonPlain.WriteRawValue(_jsonRaw, skipInputValidation: true);
            _jsonPlain.Flush();
            return _jsonBuffer.WrittenCount;
        }

        /// <summary>
        /// System.Text.Json string written in UTF-8 segments of 1,000 bytes.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("StringSegments")]
        public long Utf8JsonWriter_StringSegments()
        {
            ResetJson(_jsonPlain);
            for (var round = 0; round < 20; round++)
            {
                for (var offset = 0; offset < _utf8Text.Length; offset += 1_000)
                {
                    var length = Math.Min(1_000, _utf8Text.Length - offset);
                    _jsonPlain.WriteStringValueSegment(_utf8Text.AsSpan(offset, length), round == 19 && offset + length == _utf8Text.Length);
                }
            }

            _jsonPlain.Flush();
            return _jsonBuffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials catalog with every attribute on a line of its own (<see cref="XmlAttributeStyle.PerLine"/>).
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("AttributesPerLine")]
        public long XmlEssentials_AttributesPerLine()
        {
            Reset(_perLine);
            WriteEssentialsWideCatalog(_perLine);
            _perLine.Flush();
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials value written as base64 in segments of 3,000 bytes.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("Base64Segments")]
        public long XmlEssentials_Base64Segments()
        {
            Reset(_plain);
            _plain.WriteStartElement("data");
            for (var offset = 0; offset < _binary.Length; offset += 3_000)
            {
                var length = Math.Min(3_000, _binary.Length - offset);
                _plain.WriteBase64StringSegment(_binary.AsSpan(offset, length), offset + length == _binary.Length);
            }

            _plain.WriteEndElement();
            _plain.Flush();
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials catalog of 200 items with element names as <see cref="XmlEncodedText"/>.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("EncodedNames")]
        public long XmlEssentials_EncodedNames()
        {
            Reset(_plain);
            _plain.WriteStartElement(XmlCatalog);
            for (var i = 0; i < Count; i++)
            {
                _plain.WriteStartElement(XmlItem);
                _plain.WriteAttributeNumber("id", i);
                _plain.WriteElementString(XmlName, "Widget");
                _plain.WriteElementNumber(XmlQty, i);
                _plain.WriteElementNumber(XmlPrice, 9.99d);
                _plain.WriteEndElement();
            }

            _plain.WriteEndElement();
            _plain.Flush();
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials catalog of 200 items with element names as strings, beside the encoded names.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("EncodedNames")]
        public long XmlEssentials_StringNames()
        {
            Reset(_plain);
            _plain.WriteStartElement("catalog");
            for (var i = 0; i < Count; i++)
            {
                _plain.WriteStartElement("item");
                _plain.WriteAttributeNumber("id", i);
                _plain.WriteElementString("name", "Widget");
                _plain.WriteElementNumber("qty", i);
                _plain.WriteElementNumber("price", 9.99d);
                _plain.WriteEndElement();
            }

            _plain.WriteEndElement();
            _plain.Flush();
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials indented catalog of 200 items, with a comment before each item.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("IndentedWrite")]
        public long XmlEssentials_IndentedWrite()
        {
            Reset(_indented);
            _indented.WriteStartElement("catalog");
            for (var i = 0; i < Count; i++)
            {
                _indented.WriteComment("an item");
                _indented.WriteStartElement("item");
                _indented.WriteAttributeNumber("id", i);
                _indented.WriteElementString("name", "Widget");
                _indented.WriteElementNumber("qty", i);
                _indented.WriteElementNumber("price", 9.99d);
                _indented.WriteEndElement();
            }

            _indented.WriteEndElement();
            _indented.Flush();
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials text that holds line breaks, each rewritten as CR LF, in an XML 1.0 document.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("NewLineText")]
        public long XmlEssentials_NewLineText_CrLf()
        {
            return WriteNotes(_crLf10);
        }

        /// <summary>
        /// XmlEssentials text that holds line breaks, each rewritten as CR LF, in an XML 1.1 document.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("NewLineText")]
        public long XmlEssentials_NewLineText_CrLf_Xml11()
        {
            return WriteNotes(_crLf11);
        }

        /// <summary>
        /// XmlEssentials text that holds line breaks, written as they are: the cost with no rewriting.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("NewLineText")]
        public long XmlEssentials_NewLineText_Lf()
        {
            return WriteNotes(_lf);
        }

        /// <summary>
        /// XmlEssentials raw value, checked before it is written.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("RawValue")]
        public long XmlEssentials_RawValue()
        {
            Reset(_plain);
            _plain.WriteStartElement("root");
            _plain.WriteRawValue(_rawXml);
            _plain.WriteEndElement();
            _plain.Flush();
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials raw value, written without being checked.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("RawValue")]
        public long XmlEssentials_RawValue_SkipValidation()
        {
            Reset(_plain);
            _plain.WriteStartElement("root");
            _plain.WriteRawValue(_rawXml, skipInputValidation: true);
            _plain.WriteEndElement();
            _plain.Flush();
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials text written in UTF-8 segments of 1,000 bytes.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("StringSegments")]
        public long XmlEssentials_StringSegments()
        {
            Reset(_plain);
            _plain.WriteStartElement("text");
            for (var round = 0; round < 20; round++)
            {
                for (var offset = 0; offset < _utf8Text.Length; offset += 1_000)
                {
                    var length = Math.Min(1_000, _utf8Text.Length - offset);
                    _plain.WriteStringValueSegment(_utf8Text.AsSpan(offset, length), round == 19 && offset + length == _utf8Text.Length);
                }
            }

            _plain.WriteEndElement();
            _plain.Flush();
            return _buffer.WrittenCount;
        }

        #endregion

        #region Private Methods

        private void Reset(Utf8XmlWriter writer)
        {
            _buffer.ResetWrittenCount();
            writer.Reset(_buffer);
        }

        private void ResetJson(Utf8JsonWriter writer)
        {
            _jsonBuffer.ResetWrittenCount();
            writer.Reset(_jsonBuffer);
        }

        private long WriteBcl(XmlWriterSettings settings, Action<XmlWriter> write)
        {
            _stream.Position = 0;
            _stream.SetLength(0);
            using (var writer = XmlWriter.Create(_stream, settings))
            {
                write(writer);
            }

            return _stream.Length;
        }

        private static void WriteBclCatalog(XmlWriter writer)
        {
            writer.WriteStartElement("catalog");
            for (var i = 0; i < Count; i++)
            {
                WriteBclItem(writer, i);
            }

            writer.WriteEndElement();
        }

        private static void WriteBclCommentedCatalog(XmlWriter writer)
        {
            writer.WriteStartElement("catalog");
            for (var i = 0; i < Count; i++)
            {
                writer.WriteComment("an item");
                WriteBclItem(writer, i);
            }

            writer.WriteEndElement();
        }

        private static void WriteBclItem(XmlWriter writer, int i)
        {
            writer.WriteStartElement("item");
            writer.WriteStartAttribute("id");
            writer.WriteValue(i);
            writer.WriteEndAttribute();
            writer.WriteElementString("name", "Widget");
            writer.WriteStartElement("qty");
            writer.WriteValue(i);
            writer.WriteEndElement();
            writer.WriteStartElement("price");
            writer.WriteValue(9.99d);
            writer.WriteEndElement();
            writer.WriteEndElement();
        }

        private static void WriteBclWideCatalog(XmlWriter writer)
        {
            writer.WriteStartElement("catalog");
            for (var i = 0; i < Count; i++)
            {
                writer.WriteStartElement("item");
                writer.WriteStartAttribute("id");
                writer.WriteValue(i);
                writer.WriteEndAttribute();
                writer.WriteAttributeString("name", "Widget");
                writer.WriteStartAttribute("qty");
                writer.WriteValue(i);
                writer.WriteEndAttribute();
                writer.WriteAttributeString("currency", "USD");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static void WriteEssentialsCatalog(Utf8XmlWriter writer)
        {
            writer.WriteStartElement("catalog");
            for (var i = 0; i < Count; i++)
            {
                writer.WriteStartElement("item");
                writer.WriteAttributeNumber("id", i);
                writer.WriteElementString("name", "Widget");
                writer.WriteElementNumber("qty", i);
                writer.WriteElementNumber("price", 9.99d);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static void WriteEssentialsWideCatalog(Utf8XmlWriter writer)
        {
            writer.WriteStartElement("catalog");
            for (var i = 0; i < Count; i++)
            {
                writer.WriteStartElement("item");
                writer.WriteAttributeNumber("id", i);
                writer.WriteAttributeString("name", "Widget");
                writer.WriteAttributeNumber("qty", i);
                writer.WriteAttributeString("currency", "USD");
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static void WriteJsonCatalog(Utf8JsonWriter writer, bool commented)
        {
            writer.WriteStartArray();
            for (var i = 0; i < Count; i++)
            {
                if (commented)
                {
                    writer.WriteCommentValue("an item");
                }

                writer.WriteStartObject();
                writer.WriteNumber(JsonId, i);
                writer.WriteString(JsonName, "Widget");
                writer.WriteNumber(JsonQty, i);
                writer.WriteNumber(JsonPrice, 9.99d);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private long WriteNotes(Utf8XmlWriter writer)
        {
            Reset(writer);
            writer.WriteStartElement("notes");
            for (var i = 0; i < Count; i++)
            {
                writer.WriteElementString("note", _lines);
            }

            writer.WriteEndElement();
            writer.Flush();
            return _buffer.WrittenCount;
        }

        #endregion

    }

}
