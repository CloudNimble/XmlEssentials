using System;
using System.IO;
using System.Text;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Measures the parts of <see cref="Utf8XmlReader"/> that the drain benchmarks do not reach: a document that
    /// arrives in blocks and is resumed from a state, skipping the content of an element, and a start tag with
    /// very many attributes.
    /// </summary>
    /// <remarks>
    /// The BCL <see cref="XmlReader"/> is the baseline of each category. It reads a stream in blocks of its own,
    /// which is its counterpart of a resumed read.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class Utf8XmlReaderFlowBenchmarks
    {

        #region Fields

        private const int BlockSize = 4_096;
        private XmlReaderSettings _bclSettings = null!;
        private byte[] _catalog = null!;
        private byte[] _nested = null!;
        private byte[] _wide1000 = null!;
        private byte[] _wide5000 = null!;
        private XmlReaderOptions _wideOptions;

        #endregion

        #region Public Methods

        /// <summary>
        /// BCL drain of the catalog from a stream, which the reader takes in blocks of its own.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ResumedDrain")]
        public int BclXmlReader_ResumedDrain()
        {
            using var stream = new MemoryStream(_catalog, writable: false);
            using var reader = XmlReader.Create(stream, _bclSettings);
            var count = 0;
            while (reader.Read())
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// BCL read of the top-level items, skipping the content of each with <see cref="XmlReader.Skip"/>.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Skip")]
        public int BclXmlReader_Skip()
        {
            using var stream = new MemoryStream(_nested, writable: false);
            using var reader = XmlReader.Create(stream, _bclSettings);
            var count = 0;
            reader.MoveToContent();
            reader.ReadStartElement();
            while (reader.NodeType is XmlNodeType.Element)
            {
                count++;
                reader.Skip();
            }

            return count;
        }

        /// <summary>
        /// BCL read of one start tag with 1,000 attributes.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WideElement1000")]
        public int BclXmlReader_WideElement1000()
        {
            return DrainBcl(_wide1000);
        }

        /// <summary>
        /// BCL read of one start tag with 5,000 attributes.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WideElement5000")]
        public int BclXmlReader_WideElement5000()
        {
            return DrainBcl(_wide5000);
        }

        /// <summary>
        /// Builds the documents.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            _bclSettings = new XmlReaderSettings { CloseInput = false };
            _wideOptions = new XmlReaderOptions { MaxAttributeCount = 10_000 };

            var catalog = new StringBuilder("<catalog>");
            for (var i = 0; i < 2_000; i++)
            {
                catalog.Append("<item id=\"").Append(i).Append("\"><name>Widget &amp; Co ").Append(i).Append("</name><qty>").Append(i).Append("</qty><price>9.99</price></item>");
            }

            _catalog = Encoding.UTF8.GetBytes(catalog.Append("</catalog>").ToString());

            // Each top-level item holds a subtree of thirty elements, which is what a skip passes over.
            var nested = new StringBuilder("<orders>");
            for (var i = 0; i < 200; i++)
            {
                nested.Append("<order id=\"").Append(i).Append("\">");
                for (var line = 0; line < 10; line++)
                {
                    nested.Append("<line sku=\"SKU-").Append(line).Append("\"><qty>").Append(line).Append("</qty><note>ring &amp; wait</note></line>");
                }

                nested.Append("</order>");
            }

            _nested = Encoding.UTF8.GetBytes(nested.Append("</orders>").ToString());
            _wide1000 = Wide(1_000);
            _wide5000 = Wide(5_000);
        }

        /// <summary>
        /// XmlEssentials drain of the catalog in one block, beside the resumed read.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ResumedDrain")]
        public int XmlEssentials_OneBlockDrain()
        {
            var reader = new Utf8XmlReader(_catalog);
            var count = 0;
            while (reader.Read())
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// XmlEssentials drain of the catalog in blocks of 4,096 bytes, each resumed from the state of the last.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ResumedDrain")]
        public int XmlEssentials_ResumedDrain()
        {
            var document = _catalog.AsSpan();
            var state = default(XmlReaderState);
            var start = 0;
            var size = BlockSize;
            var count = 0;
            while (true)
            {
                var end = Math.Min(start + size, document.Length);
                var isFinalBlock = end == document.Length;
                var reader = new Utf8XmlReader(document[start..end], isFinalBlock, state);
                while (reader.Read())
                {
                    count++;
                }

                if (isFinalBlock)
                {
                    return count;
                }

                // What the reader did not consume is the start of a token the block cut. It is given again at
                // the head of the next block, as a caller that reads a stream would give it.
                var consumed = (int)reader.BytesConsumed;
                size = consumed == 0 ? size * 2 : BlockSize;
                start += consumed;
                state = reader.CurrentState;
            }
        }

        /// <summary>
        /// XmlEssentials read of the top-level items, skipping the content of each with <see cref="Utf8XmlReader.Skip"/>.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("Skip")]
        public int XmlEssentials_Skip()
        {
            var reader = new Utf8XmlReader(_nested);
            var count = 0;
            reader.Read();
            while (reader.Read() && reader.TokenType is XmlTokenType.StartElement)
            {
                count++;
                reader.Skip();
            }

            return count;
        }

        /// <summary>
        /// XmlEssentials read of the top-level items with <see cref="Utf8XmlReader.TrySkip"/>, in blocks of 4,096
        /// bytes, so that a skip the block cuts short is taken back and tried again.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("Skip")]
        public int XmlEssentials_TrySkip_Resumed()
        {
            var document = _nested.AsSpan();
            var state = default(XmlReaderState);
            var start = 0;
            var size = BlockSize;
            var count = 0;
            while (true)
            {
                var end = Math.Min(start + size, document.Length);
                var isFinalBlock = end == document.Length;
                var reader = new Utf8XmlReader(document[start..end], isFinalBlock, state);
                while (true)
                {
                    // The copy is where the next block starts when the block ends inside this item.
                    var before = reader;
                    if (!reader.Read())
                    {
                        break;
                    }

                    if (reader.TokenType is XmlTokenType.StartElement && reader.CurrentDepth == 1)
                    {
                        if (!reader.TrySkip())
                        {
                            reader = before;
                            break;
                        }

                        count++;
                    }
                }

                if (isFinalBlock)
                {
                    return count;
                }

                var consumed = (int)reader.BytesConsumed;
                size = consumed == 0 ? size * 2 : BlockSize;
                start += consumed;
                state = reader.CurrentState;
            }
        }

        /// <summary>
        /// XmlEssentials read of one start tag with 1,000 attributes.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WideElement1000")]
        public int XmlEssentials_WideElement1000()
        {
            return DrainEssentials(_wide1000);
        }

        /// <summary>
        /// XmlEssentials read of one start tag with 5,000 attributes.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WideElement5000")]
        public int XmlEssentials_WideElement5000()
        {
            return DrainEssentials(_wide5000);
        }

        #endregion

        #region Private Methods

        private int DrainBcl(byte[] xml)
        {
            using var stream = new MemoryStream(xml, writable: false);
            using var reader = XmlReader.Create(stream, _bclSettings);
            var count = 0;
            while (reader.Read())
            {
                count += reader.AttributeCount;
            }

            return count;
        }

        private int DrainEssentials(byte[] xml)
        {
            var reader = new Utf8XmlReader(xml, _wideOptions);
            var count = 0;
            while (reader.Read())
            {
                count += reader.AttributeCount;
            }

            return count;
        }

        private static byte[] Wide(int attributes)
        {
            var builder = new StringBuilder("<row");
            for (var i = 0; i < attributes; i++)
            {
                builder.Append(" a").Append(i).Append("=\"").Append(i).Append('"');
            }

            return Encoding.UTF8.GetBytes(builder.Append("/>").ToString());
        }

        #endregion

    }

}
