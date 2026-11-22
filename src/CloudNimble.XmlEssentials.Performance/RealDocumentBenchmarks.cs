using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;
using BclXmlDocument = System.Xml.XmlDocument;
using EssentialsXmlDocument = CloudNimble.Text.Xml.XmlDocument;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Compares the reader and DOM to the BCL on real documents from the test fixtures.
    /// </summary>
    /// <remarks>
    /// The synthetic catalog is ASCII with one short prefix. These documents have what it lacks: entity
    /// references, multi-byte text, many namespaces with long prefixes, indentation, CDATA, and mixed content.
    /// Both sides keep whitespace and comments, so they report the same nodes. The write category replays a
    /// parsed document, which is the only way to drive the writer with real prefixed names and attributes
    /// through public UTF-8 overloads.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory, BenchmarkLogicalGroupRule.ByParams)]
    public class RealDocumentBenchmarks
    {

        #region Fields

        private BclXmlDocument _bclDocument = null!;
        private MemoryStream _bclOutput = null!;
        private XmlWriterSettings _bclWriterSettings = null!;
        private ArrayBufferWriter<byte> _buffer = null!;
        private byte[] _document = null!;
        private EssentialsXmlDocument _parsed = null!;
        private XmlReaderSettings _settings = null!;
        private MemoryStream _stream = null!;
        private Utf8XmlWriter _writer = null!;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the fixture file under <c>Fixtures/quick-xml</c>.
        /// </summary>
        [Params("sample_rss.xml", "libreoffice_document.fodt", "document.xml", "rpm_primary.xml")]
        public string File { get; set; } = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// BCL <see cref="XmlReader"/> drain.
        /// </summary>
        /// <returns>The number of nodes.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("RealDrain")]
        public int BclXmlReader_Drain()
        {
            _stream.Position = 0;
            using var reader = XmlReader.Create(_stream, _settings);
            var count = 0;
            while (reader.Read())
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// BCL <see cref="BclXmlDocument"/> load.
        /// </summary>
        /// <returns>The number of attributes on the root.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("RealDomParse")]
        public int BclXmlDocument_Parse()
        {
            _stream.Position = 0;
            using var reader = XmlReader.Create(_stream, _settings);
            var document = new BclXmlDocument { PreserveWhitespace = true };
            document.Load(reader);
            return document.DocumentElement!.Attributes.Count;
        }

        /// <summary>
        /// BCL write of the parsed document through an <see cref="XmlWriter"/>.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("RealDomWrite")]
        public void BclXmlDocument_Write()
        {
            _bclOutput.Position = 0;
            _bclOutput.SetLength(0);
            using var writer = XmlWriter.Create(_bclOutput, _bclWriterSettings);
            _bclDocument.WriteTo(writer);
            writer.Flush();
        }

        /// <summary>
        /// Returns pooled buffers.
        /// </summary>
        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _parsed.Dispose();
            _writer.Dispose();
            _bclOutput.Dispose();
        }

        /// <summary>
        /// Loads the fixture, one parsed document per library, and reused destinations.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            _document = System.IO.File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "quick-xml", File));
            _stream = new MemoryStream(_document, writable: false);
            _settings = new XmlReaderSettings
            {
                CheckCharacters = true,
                CloseInput = false,
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = false,
                IgnoreWhitespace = false,
                XmlResolver = null,
            };
            _bclWriterSettings = new XmlWriterSettings
            {
                CloseOutput = false,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = false,
                OmitXmlDeclaration = true,
            };
            _bclOutput = new MemoryStream(_document.Length * 2);
            using (var reader = XmlReader.Create(_stream, _settings))
            {
                _bclDocument = new BclXmlDocument { PreserveWhitespace = true };
                _bclDocument.Load(reader);
            }

            _buffer = new ArrayBufferWriter<byte>(_document.Length * 2);
            _writer = new Utf8XmlWriter(_buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            _parsed = EssentialsXmlDocument.Parse(_document, new XmlDocumentOptions
            {
                CommentHandling = XmlCommentHandling.Allow,
                WhitespaceHandling = XmlWhitespaceHandling.Significant,
            });
        }

        /// <summary>
        /// XmlEssentials drain without materializing strings.
        /// </summary>
        /// <returns>The number of tokens.</returns>
        [Benchmark]
        [BenchmarkCategory("RealDrain")]
        public int XmlEssentials_Drain()
        {
            var reader = new Utf8XmlReader(_document, new XmlReaderOptions
            {
                CommentHandling = XmlCommentHandling.Allow,
                WhitespaceHandling = XmlWhitespaceHandling.Significant,
            });
            var count = 0;
            while (reader.Read())
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// XmlEssentials parse and dispose.
        /// </summary>
        /// <returns>The number of attributes on the root.</returns>
        [Benchmark]
        [BenchmarkCategory("RealDomParse")]
        public int XmlEssentials_Parse()
        {
            using var document = EssentialsXmlDocument.Parse(_document, new XmlDocumentOptions
            {
                CommentHandling = XmlCommentHandling.Allow,
                WhitespaceHandling = XmlWhitespaceHandling.Significant,
            });
            return document.RootElement.AttributeCount;
        }

        /// <summary>
        /// XmlEssentials write of the parsed document, reusing the writer with <see cref="Utf8XmlWriter.Reset()"/>.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("RealDomWrite")]
        public void XmlEssentials_Write()
        {
            _writer.Reset();
            _buffer.Clear();
            _parsed.WriteTo(_writer);
            _writer.Flush();
        }

        #endregion

    }

}
