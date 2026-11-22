using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using BclXmlSerializer = System.Xml.Serialization.XmlSerializer;
using EssentialsXmlSerializer = CloudNimble.Text.Xml.XmlSerializer;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Measures a sequence of 1,000 integers read and written as a stream of values, and splits the cost of
    /// reading a single value into its parts.
    /// </summary>
    /// <remarks>
    /// Sequence rows: BCL has no streaming call, so its baseline is a cached
    /// <see cref="BclXmlSerializer"/> over <c>int[]</c>; <see cref="JsonSerializer"/> is a sidecar using the same
    /// asynchronous sequence calls as XmlEssentials. Every reader gets a <see cref="MemoryStream"/> rewound per
    /// operation. Breakdown rows have no baseline: they compare the tokenizer alone with the serializer on the
    /// same bytes, with and without an XML declaration.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlSerializerSequenceBenchmarks
    {

        #region Fields

        private const int Count = 1_000;
        private BclXmlSerializer _bcl = null!;
        private MemoryStream _bclInput = null!;
        private MemoryStream _bclOutput = null!;
        private XmlReaderSettings _bclReaderSettings = null!;
        private XmlWriterSettings _bclWriterSettings = null!;
        private byte[] _declared = null!;
        private MemoryStream _jsonInput = null!;
        private MemoryStream _output = null!;
        private byte[] _plain = null!;
        private XmlTypeInfo<int> _typeInfo = null!;
        private int[] _values = null!;
        private MemoryStream _xmlInput = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// BCL read of the whole array.
        /// </summary>
        /// <returns>The sum of the values.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadSequence")]
        public long Bcl_ReadArray()
        {
            _bclInput.Position = 0;
            using var reader = XmlReader.Create(_bclInput, _bclReaderSettings);
            var sum = 0L;
            foreach (var value in (int[])_bcl.Deserialize(reader)!)
            {
                sum += value;
            }

            return sum;
        }

        /// <summary>
        /// BCL write of the whole array.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteSequence")]
        public long Bcl_WriteArray()
        {
            _bclOutput.SetLength(0);
            using var writer = XmlWriter.Create(_bclOutput, _bclWriterSettings);
            _bcl.Serialize(writer, _values);
            writer.Flush();
            return _bclOutput.Length;
        }

        /// <summary>
        /// Releases the reused streams.
        /// </summary>
        [GlobalCleanup]
        public void Cleanup()
        {
            _bclInput.Dispose();
            _bclOutput.Dispose();
            _jsonInput.Dispose();
            _output.Dispose();
            _xmlInput.Dispose();
        }

        /// <summary>
        /// The tokenizer alone on a document with a declaration: construct, read to the end, parse the integer.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadInt32Parts")]
        public int Reader_Declared()
        {
            return Drain(_declared);
        }

        /// <summary>
        /// The tokenizer alone on a document without a declaration.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadInt32Parts")]
        public int Reader_Plain()
        {
            return Drain(_plain);
        }

        /// <summary>
        /// The serializer on a document with a declaration, type info resolved from the default options.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadInt32Parts")]
        public int Serializer_Declared()
        {
            return EssentialsXmlSerializer.Deserialize<int>(_declared);
        }

        /// <summary>
        /// The serializer on a document without a declaration, type info resolved from the default options.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadInt32Parts")]
        public int Serializer_Plain()
        {
            return EssentialsXmlSerializer.Deserialize<int>(_plain);
        }

        /// <summary>
        /// The serializer on a document without a declaration, with the type info supplied by the caller.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadInt32Parts")]
        public int Serializer_Plain_TypeInfo()
        {
            return EssentialsXmlSerializer.Deserialize(_plain, _typeInfo);
        }

        /// <summary>
        /// Builds the payloads once.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _values = Enumerable.Range(0, Count).ToArray();
            _typeInfo = XmlSerializerOptions.Default.GetTypeInfo<int>();
            _declared = EssentialsXmlSerializer.SerializeToUtf8Bytes(12345);
            _plain = "<Int32>12345</Int32>"u8.ToArray();
            _bcl = new BclXmlSerializer(typeof(int[]));
            _bclReaderSettings = new XmlReaderSettings { CloseInput = false };
            _bclWriterSettings = new XmlWriterSettings { CloseOutput = false, Encoding = new UTF8Encoding(false) };
            _bclOutput = new MemoryStream(65_536);
            Bcl_WriteArray();
            _bclInput = new MemoryStream(_bclOutput.ToArray());
            _jsonInput = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(_values));
            _output = new MemoryStream(65_536);
            XmlEssentials_WriteSequence().GetAwaiter().GetResult();
            _xmlInput = new MemoryStream(_output.ToArray());
        }

        /// <summary>
        /// System.Text.Json read of the sequence, one value at a time.
        /// </summary>
        /// <returns>The sum of the values.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadSequence")]
        public async Task<long> Stj_ReadSequence()
        {
            _jsonInput.Position = 0;
            var sum = 0L;
            await foreach (var value in JsonSerializer.DeserializeAsyncEnumerable<int>(_jsonInput))
            {
                sum += value;
            }

            return sum;
        }

        /// <summary>
        /// System.Text.Json write of the sequence, one value at a time.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteSequence")]
        public async Task<long> Stj_WriteSequence()
        {
            _output.SetLength(0);
            await JsonSerializer.SerializeAsync(_output, Values());
            return _output.Length;
        }

        /// <summary>
        /// XmlEssentials read of the sequence, one value at a time.
        /// </summary>
        /// <returns>The sum of the values.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadSequence")]
        public async Task<long> XmlEssentials_ReadSequence()
        {
            _xmlInput.Position = 0;
            var sum = 0L;
            await foreach (var value in EssentialsXmlSerializer.DeserializeAsyncEnumerable(_xmlInput, _typeInfo))
            {
                sum += value;
            }

            return sum;
        }

        /// <summary>
        /// XmlEssentials write of the sequence, one value at a time.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteSequence")]
        public async Task<long> XmlEssentials_WriteSequence()
        {
            _output.SetLength(0);
            await EssentialsXmlSerializer.SerializeAsyncEnumerable(_output, Values(), _typeInfo);
            return _output.Length;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reads <paramref name="xml"/> to its end with a new reader and parses the one text node.
        /// </summary>
        /// <param name="xml">The document.</param>
        /// <returns>The value.</returns>
        private static int Drain(byte[] xml)
        {
            var reader = new Utf8XmlReader(xml);
            var value = 0;
            while (reader.Read())
            {
                if (reader.TokenType is XmlTokenType.Text)
                {
                    value = reader.GetInt32();
                }
            }

            return value;
        }

        /// <summary>
        /// Yields the values without awaiting anything.
        /// </summary>
        /// <returns>The values.</returns>
        private async IAsyncEnumerable<int> Values()
        {
            foreach (var value in _values)
            {
                yield return value;
            }

            await Task.CompletedTask;
        }

        #endregion

    }

}
