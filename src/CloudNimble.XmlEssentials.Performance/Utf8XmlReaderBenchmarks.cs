using System.Text;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Compares <see cref="Utf8XmlReader"/> to BCL <see cref="XmlReader"/> on the same UTF-8 payloads.
    /// </summary>
    /// <remarks>
    /// Settings match XmlEssentials defaults: comments skipped, insignificant whitespace skipped, DTDs prohibited.
    /// BCL reuses a <see cref="MemoryStream"/> with <see cref="XmlReaderSettings.CloseInput"/> false so the
    /// comparison is the tokenizer, not stream construction. XmlEssentials reads the byte span directly.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class Utf8XmlReaderBenchmarks
    {

        #region Fields

        private byte[] _catalog = null!;
        private MemoryStream _catalogStream = null!;
        private XmlReaderSettings _settings = null!;
        private byte[] _tiny = null!;
        private MemoryStream _tinyStream = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// BCL <see cref="XmlReader"/> drain of the catalog.
        /// </summary>
        /// <returns>The number of nodes reported by <see cref="XmlReader.Read"/>.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("CatalogDrain")]
        public int BclXmlReader_DrainCatalog()
        {
            return DrainBcl(_catalogStream);
        }

        /// <summary>
        /// BCL <see cref="XmlReader"/> drain that parses integer text with <see cref="int.TryParse(string, out int)"/>.
        /// </summary>
        /// <returns>The last integer text node.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("CatalogGetInt32")]
        public int BclXmlReader_DrainCatalogGetInt32()
        {
            _catalogStream.Position = 0;
            using var reader = XmlReader.Create(_catalogStream, _settings);
            var qty = 0;
            while (reader.Read())
            {
                if (reader.NodeType is XmlNodeType.Text && int.TryParse(reader.Value, out var value))
                {
                    qty = value;
                }
            }

            return qty;
        }

        /// <summary>
        /// BCL <see cref="XmlReader"/> drain of the tiny document.
        /// </summary>
        /// <returns>The number of nodes reported by <see cref="XmlReader.Read"/>.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("TinyDrain")]
        public int BclXmlReader_DrainTiny()
        {
            return DrainBcl(_tinyStream);
        }

        /// <summary>
        /// Builds payloads and a reused BCL reader pipeline.
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
            _tinyStream = new MemoryStream(_tiny, writable: false);
            _catalogStream = new MemoryStream(_catalog, writable: false);
            _settings = new XmlReaderSettings
            {
                CheckCharacters = true,
                CloseInput = false,
                DtdProcessing = DtdProcessing.Prohibit,
                IgnoreComments = true,
                IgnoreWhitespace = true,
                XmlResolver = null,
            };
        }

        /// <summary>
        /// XmlEssentials drain of the catalog without materializing strings.
        /// </summary>
        /// <returns>The number of tokens.</returns>
        [Benchmark]
        [BenchmarkCategory("CatalogDrain")]
        public int XmlEssentials_DrainCatalog()
        {
            return DrainXmlEssentials(_catalog);
        }

        /// <summary>
        /// XmlEssentials drain that parses integer text with <see cref="Utf8XmlReader.TryGetInt32"/>.
        /// </summary>
        /// <returns>The last integer text node.</returns>
        [Benchmark]
        [BenchmarkCategory("CatalogGetInt32")]
        public int XmlEssentials_DrainCatalogGetInt32()
        {
            var reader = new Utf8XmlReader(_catalog);
            var qty = 0;
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.Text && reader.TryGetInt32(out var value))
                {
                    qty = value;
                }
            }

            return qty;
        }

        /// <summary>
        /// XmlEssentials drain of the tiny document without materializing strings.
        /// </summary>
        /// <returns>The number of tokens.</returns>
        [Benchmark]
        [BenchmarkCategory("TinyDrain")]
        public int XmlEssentials_DrainTiny()
        {
            return DrainXmlEssentials(_tiny);
        }

        #endregion

        #region Private Methods

        private int DrainBcl(MemoryStream stream)
        {
            stream.Position = 0;
            using var reader = XmlReader.Create(stream, _settings);
            var count = 0;
            while (reader.Read())
            {
                count++;
            }

            return count;
        }

        private static int DrainXmlEssentials(byte[] utf8)
        {
            var reader = new Utf8XmlReader(utf8);
            var count = 0;
            while (reader.Read())
            {
                count++;
            }

            return count;
        }

        #endregion

    }

}
