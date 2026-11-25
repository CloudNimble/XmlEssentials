using System.Buffers;
using System.IO;
using System.Text;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Compares <see cref="Utf8XmlWriter"/> to BCL <see cref="XmlWriter"/> on the same catalog shape.
    /// </summary>
    /// <remarks>
    /// XmlEssentials reuses one writer with <see cref="Utf8XmlWriter.Reset"/> (STJ <c>Utf8JsonWriter.Reset</c>).
    /// BCL <see cref="XmlWriter"/> has no reset, so those benches construct a writer per operation.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class Utf8XmlWriterBenchmarks
    {

        #region Fields

        private ArrayBufferWriter<byte> _buffer = null!;
        private XmlWriterSettings _settings = null!;
        private MemoryStream _stream = null!;
        private Utf8XmlWriter _writer = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// BCL <see cref="XmlWriter"/> catalog of 200 items.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("CatalogWrite")]
        public void BclXmlWriter_WriteCatalog()
        {
            _stream.Position = 0;
            _stream.SetLength(0);
            using var writer = XmlWriter.Create(_stream, _settings);
            WriteBclCatalog(writer);
            writer.Flush();
        }

        /// <summary>
        /// BCL <see cref="XmlWriter"/> tiny document.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("TinyWrite")]
        public void BclXmlWriter_WriteTiny()
        {
            _stream.Position = 0;
            _stream.SetLength(0);
            using var writer = XmlWriter.Create(_stream, _settings);
            writer.WriteStartElement("root");
            writer.WriteAttributeString("id", "1");
            writer.WriteStartElement("n");
            writer.WriteValue(42);
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.Flush();
        }

        /// <summary>
        /// Returns pooled writer buffers.
        /// </summary>
        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _writer.Dispose();
            _stream.Dispose();
        }

        /// <summary>
        /// Builds reused destinations and a <see cref="Utf8XmlWriter"/> for <see cref="Utf8XmlWriter.Reset"/>.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            _buffer = new ArrayBufferWriter<byte>(32_768);
            _stream = new MemoryStream(32_768);
            _settings = new XmlWriterSettings
            {
                CloseOutput = false,
                Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
                Indent = false,
                OmitXmlDeclaration = true,
            };
            _writer = new Utf8XmlWriter(_buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
        }

        /// <summary>
        /// XmlEssentials catalog of 200 items, reusing the writer with <see cref="Utf8XmlWriter.Reset"/>.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("CatalogWrite")]
        public void XmlEssentials_WriteCatalog()
        {
            _writer.Reset();
            _buffer.ResetWrittenCount();
            WriteEssentialsCatalog(_writer);
        }

        /// <summary>
        /// XmlEssentials tiny document, reusing the writer with <see cref="Utf8XmlWriter.Reset"/>.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("TinyWrite")]
        public void XmlEssentials_WriteTiny()
        {
            _writer.Reset();
            _buffer.ResetWrittenCount();
            _writer.WriteStartElement("root");
            _writer.WriteAttributeString("id", "1");
            _writer.WriteElementNumber("n", 42);
            _writer.WriteEndElement();
        }

        #endregion

        #region Private Methods

        private static void WriteEssentialsCatalog(Utf8XmlWriter writer)
        {
            writer.WriteStartElement("catalog");
            for (var i = 0; i < 200; i++)
            {
                writer.WriteStartElement("item");
                writer.WriteAttributeNumber("id", i);
                writer.WriteStartElement("name");
                writer.WriteStringValue("Widget");
                writer.WriteNumberValue(i);
                writer.WriteEndElement();
                writer.WriteElementNumber("qty", i);
                writer.WriteElementNumber("price", 9.99d);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        private static void WriteBclCatalog(XmlWriter writer)
        {
            writer.WriteStartElement("catalog");
            for (var i = 0; i < 200; i++)
            {
                writer.WriteStartElement("item");
                writer.WriteStartAttribute("id");
                writer.WriteValue(i);
                writer.WriteEndAttribute();
                writer.WriteStartElement("name");
                writer.WriteString("Widget");
                writer.WriteValue(i);
                writer.WriteEndElement();
                writer.WriteStartElement("qty");
                writer.WriteValue(i);
                writer.WriteEndElement();
                writer.WriteStartElement("price");
                writer.WriteValue(9.99d);
                writer.WriteEndElement();
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        #endregion

    }

}
