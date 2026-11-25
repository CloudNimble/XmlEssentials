using System.Buffers;
using System.IO;
using System.Text;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;
using BclXmlDocument = System.Xml.XmlDocument;
using BclXmlElement = System.Xml.XmlElement;
using EssentialsXmlDocument = CloudNimble.Text.Xml.XmlDocument;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Compares <see cref="EssentialsXmlDocument"/> to BCL <see cref="BclXmlDocument"/> on the tokenizer payloads.
    /// </summary>
    /// <remarks>
    /// Parse benches build and dispose a document per operation. Navigate and write benches reuse one parsed
    /// document on each side, so they measure the tree rather than the tokenizer. BCL reuses a
    /// <see cref="MemoryStream"/> with <see cref="XmlReaderSettings.CloseInput"/> false; XmlEssentials reuses one
    /// <see cref="Utf8XmlWriter"/> through <see cref="Utf8XmlWriter.Reset()"/>.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlDocumentBenchmarks
    {

        #region Fields

        private BclXmlDocument _bclCatalog = null!;
        private MemoryStream _bclOutput = null!;
        private XmlWriterSettings _bclWriterSettings = null!;
        private ArrayBufferWriter<byte> _buffer = null!;
        private byte[] _catalog = null!;
        private EssentialsXmlDocument _catalogDocument = null!;
        private MemoryStream _catalogStream = null!;
        private byte[] _namespaced = null!;
        private MemoryStream _namespacedStream = null!;
        private XmlReaderSettings _settings = null!;
        private byte[] _tiny = null!;
        private MemoryStream _tinyStream = null!;
        private Utf8XmlWriter _writer = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// BCL walk of every item, reading the <c>id</c> attribute and the <c>qty</c> child as integers.
        /// </summary>
        /// <returns>The sum of every id and quantity.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("DomNavigate")]
        public int BclXmlDocument_NavigateCatalog()
        {
            var sum = 0;
            for (var node = _bclCatalog.DocumentElement!.FirstChild; node is not null; node = node.NextSibling)
            {
                var item = (BclXmlElement)node;
                sum += int.Parse(item.GetAttribute("id"));
                sum += int.Parse(item["qty"]!.InnerText);
            }

            return sum;
        }

        /// <summary>
        /// BCL parse of the catalog into a <see cref="BclXmlDocument"/>.
        /// </summary>
        /// <returns>The number of items under the root.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("DomParseCatalog")]
        public int BclXmlDocument_ParseCatalog()
        {
            return LoadBcl(_catalogStream).DocumentElement!.ChildNodes.Count;
        }

        /// <summary>
        /// BCL parse of the namespaced catalog into a <see cref="BclXmlDocument"/>.
        /// </summary>
        /// <returns>The number of items under the root.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("DomParseNamespaced")]
        public int BclXmlDocument_ParseNamespaced()
        {
            return LoadBcl(_namespacedStream).DocumentElement!.ChildNodes.Count;
        }

        /// <summary>
        /// BCL parse of the tiny document into a <see cref="BclXmlDocument"/>.
        /// </summary>
        /// <returns>The number of attributes on the root.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("DomParseTiny")]
        public int BclXmlDocument_ParseTiny()
        {
            return LoadBcl(_tinyStream).DocumentElement!.Attributes.Count;
        }

        /// <summary>
        /// BCL write of the parsed catalog through an <see cref="XmlWriter"/>.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("DomWrite")]
        public void BclXmlDocument_WriteCatalog()
        {
            _bclOutput.Position = 0;
            _bclOutput.SetLength(0);
            using var writer = XmlWriter.Create(_bclOutput, _bclWriterSettings);
            _bclCatalog.WriteTo(writer);
            writer.Flush();
        }

        /// <summary>
        /// Returns pooled buffers.
        /// </summary>
        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _catalogDocument.Dispose();
            _writer.Dispose();
            _bclOutput.Dispose();
        }

        /// <summary>
        /// Builds payloads, one parsed document per library, and reused destinations.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            _tiny = "<root id=\"1\"><n>42</n></root>"u8.ToArray();
            var builder = new StringBuilder(16_384);
            builder.Append("<catalog>");
            for (var i = 0; i < 200; i++)
            {
                builder.Append("<item id=\"").Append(i).Append("\"><name>Widget").Append(i)
                    .Append("</name><qty>").Append(i).Append("</qty><price>9.99</price></item>");
            }

            builder.Append("</catalog>");
            _catalog = Encoding.UTF8.GetBytes(builder.ToString());
            var ns = new StringBuilder(16_384);
            ns.Append("<p:catalog xmlns:p=\"urn:catalog\">");
            for (var i = 0; i < 200; i++)
            {
                ns.Append("<p:item id=\"").Append(i).Append("\"><p:name>Widget").Append(i)
                    .Append("</p:name><p:qty>").Append(i).Append("</p:qty></p:item>");
            }

            ns.Append("</p:catalog>");
            _namespaced = Encoding.UTF8.GetBytes(ns.ToString());
            _tinyStream = new MemoryStream(_tiny, writable: false);
            _catalogStream = new MemoryStream(_catalog, writable: false);
            _namespacedStream = new MemoryStream(_namespaced, writable: false);
            _settings = new XmlReaderSettings
            {
                CheckCharacters = true,
                CloseInput = false,
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                XmlResolver = null,
            };
            _bclWriterSettings = new XmlWriterSettings
            {
                CloseOutput = false,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = false,
                OmitXmlDeclaration = true,
            };
            _bclOutput = new MemoryStream(32_768);
            _bclCatalog = LoadBcl(_catalogStream);
            _buffer = new ArrayBufferWriter<byte>(32_768);
            _writer = new Utf8XmlWriter(_buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            _catalogDocument = EssentialsXmlDocument.Parse(_catalog);
        }

        /// <summary>
        /// XmlEssentials walk of every item, reading the <c>id</c> attribute and the <c>qty</c> child as integers.
        /// </summary>
        /// <returns>The sum of every id and quantity.</returns>
        [Benchmark]
        [BenchmarkCategory("DomNavigate")]
        public int XmlEssentials_NavigateCatalog()
        {
            var sum = 0;
            foreach (var item in _catalogDocument.RootElement.EnumerateChildren())
            {
                sum += item.GetAttribute("id").GetInt32();
                sum += item.GetChild("qty").GetInt32();
            }

            return sum;
        }

        /// <summary>
        /// XmlEssentials parse and dispose of the catalog.
        /// </summary>
        /// <returns>The number of attributes on the root.</returns>
        [Benchmark]
        [BenchmarkCategory("DomParseCatalog")]
        public int XmlEssentials_ParseCatalog()
        {
            using var document = EssentialsXmlDocument.Parse(_catalog);
            return document.RootElement.AttributeCount;
        }

        /// <summary>
        /// XmlEssentials parse and dispose of the namespaced catalog.
        /// </summary>
        /// <returns>The number of attributes on the root.</returns>
        [Benchmark]
        [BenchmarkCategory("DomParseNamespaced")]
        public int XmlEssentials_ParseNamespaced()
        {
            using var document = EssentialsXmlDocument.Parse(_namespaced);
            return document.RootElement.AttributeCount;
        }

        /// <summary>
        /// XmlEssentials parse and dispose of the tiny document.
        /// </summary>
        /// <returns>The number of attributes on the root.</returns>
        [Benchmark]
        [BenchmarkCategory("DomParseTiny")]
        public int XmlEssentials_ParseTiny()
        {
            using var document = EssentialsXmlDocument.Parse(_tiny);
            return document.RootElement.AttributeCount;
        }

        /// <summary>
        /// XmlEssentials write of the parsed catalog, reusing the writer with <see cref="Utf8XmlWriter.Reset()"/>.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("DomWrite")]
        public void XmlEssentials_WriteCatalog()
        {
            _writer.Reset();
            _buffer.ResetWrittenCount();
            _catalogDocument.WriteTo(_writer);
            _writer.Flush();
        }

        #endregion

        #region Private Methods

        private BclXmlDocument LoadBcl(MemoryStream stream)
        {
            stream.Position = 0;
            using var reader = XmlReader.Create(stream, _settings);
            var document = new BclXmlDocument();
            document.Load(reader);
            return document;
        }

        #endregion

    }

}
