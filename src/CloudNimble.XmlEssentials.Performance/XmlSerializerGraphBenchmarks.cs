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
using CloudNimble.XmlEssentials.Performance.Models;
using BclXmlSerializer = System.Xml.Serialization.XmlSerializer;
using EssentialsXmlSerializer = CloudNimble.Text.Xml.XmlSerializer;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// The PR 9 graph: one Customer (addresses, 20 line items, tags) against cached BCL, STJ, cached
    /// XmlEssentials reflection, first-call XmlEssentials reflection, and a source-generated context.
    /// </summary>
    /// <remarks>
    /// BCL is a cached <c>XmlSerializer</c> labeled as defaults with collection wrapping unless the model
    /// attributes pin the infoset. Declaration and indent match XmlEssentials defaults (declaration on, no indent).
    /// First-call rows create a new <c>XmlSerializerOptions</c> each iteration so they include type-info bind.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlSerializerGraphBenchmarks
    {

        #region Fields

        private MemoryStream _bclOutput = null!;
        private XmlReaderSettings _bclReaderSettings = null!;
        private XmlWriterSettings _bclWriterSettings = null!;
        private BclXmlSerializer _bclSerializer = null!;
        private byte[] _bclXml = null!;
        private ArrayBufferWriter<byte> _buffer = null!;
        private XmlSerializerOptions _firstOptions = null!;
        private ArrayBufferWriter<byte> _jsonBuffer = null!;
        private Utf8JsonWriter _jsonWriter = null!;
        private byte[] _json = null!;
        private Customer _value = null!;
        private byte[] _xml = null!;
        private byte[] _xmlGenerated = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// Cached BCL <c>XmlSerializer</c> read. Collection wrapping follows the attributes on <see cref="Customer"/>.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadCustomer")]
        public Customer? Bcl_ReadCustomer()
        {
            using var stream = new MemoryStream(_bclXml, writable: false);
            using var reader = XmlReader.Create(stream, _bclReaderSettings);
            return (Customer?)_bclSerializer.Deserialize(reader);
        }

        /// <summary>
        /// Cached BCL <c>XmlSerializer</c> write.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteCustomer")]
        public long Bcl_WriteCustomer()
        {
            _bclOutput.SetLength(0);
            using (var writer = XmlWriter.Create(_bclOutput, _bclWriterSettings))
            {
                _bclSerializer.Serialize(writer, _value);
            }

            return _bclOutput.Length;
        }

        /// <summary>
        /// Releases the reused sinks.
        /// </summary>
        [GlobalCleanup]
        public void Cleanup()
        {
            _bclOutput.Dispose();
            _jsonWriter.Dispose();
        }

        /// <summary>
        /// Builds the graph, cached serializers, and payloads once.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _bclOutput = new MemoryStream(32_768);
            _bclReaderSettings = new XmlReaderSettings { CloseInput = false };
            _bclWriterSettings = new XmlWriterSettings { CloseOutput = false, Encoding = new UTF8Encoding(false) };
            _buffer = new ArrayBufferWriter<byte>(32_768);
            _jsonBuffer = new ArrayBufferWriter<byte>(32_768);
            _jsonWriter = new Utf8JsonWriter(_jsonBuffer);
            _value = new Customer
            {
                Billing = new Address { City = "Springfield", PostalCode = "49007", Street = "742 Evergreen Terrace" },
                Lines = [.. Enumerable.Range(0, 20).Select(i => new OrderLine { Price = 9.99m + i, Quantity = i + 1, Sku = $"SKU-{i:D5}" })],
                Name = "Homer & Marge <Simpson>",
                Shipping = new Address { City = "Shelbyville", PostalCode = "49008", Street = "1 Main Street" },
                Tags = ["retail", "priority", "net-30", "tax-exempt"],
            };
            _bclSerializer = new BclXmlSerializer(typeof(Customer));
            Bcl_WriteCustomer();
            _bclXml = _bclOutput.ToArray();
            _json = JsonSerializer.SerializeToUtf8Bytes(_value);
            _xml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_value);
            _xmlGenerated = EssentialsXmlSerializer.SerializeToUtf8Bytes(_value, GraphXmlContext.Default.Customer);
        }

        /// <summary>
        /// New options (and type-info bind) each iteration, then read.
        /// </summary>
        [IterationSetup(Target = nameof(XmlEssentials_ReadCustomer_FirstCall))]
        public void SetupFirstCallRead()
        {
            _firstOptions = new XmlSerializerOptions();
        }

        /// <summary>
        /// New options (and type-info bind) each iteration, then write.
        /// </summary>
        [IterationSetup(Target = nameof(XmlEssentials_WriteCustomer_FirstCall))]
        public void SetupFirstCallWrite()
        {
            _firstOptions = new XmlSerializerOptions();
        }

        /// <summary>
        /// System.Text.Json read of the same graph. Sidecar, not a quality baseline.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadCustomer")]
        public Customer? Stj_ReadCustomer()
        {
            return JsonSerializer.Deserialize<Customer>(_json);
        }

        /// <summary>
        /// System.Text.Json write of the same graph. Sidecar, not a quality baseline.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteCustomer")]
        public long Stj_WriteCustomer()
        {
            _jsonBuffer.ResetWrittenCount();
            _jsonWriter.Reset(_jsonBuffer);
            JsonSerializer.Serialize(_jsonWriter, _value);
            return _jsonBuffer.WrittenCount;
        }

        /// <summary>
        /// Cached XmlEssentials reflection read.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadCustomer")]
        public Customer? XmlEssentials_ReadCustomer()
        {
            return EssentialsXmlSerializer.Deserialize<Customer>(_xml);
        }

        /// <summary>
        /// First-call XmlEssentials reflection read: type-info bind plus deserialize.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadCustomer")]
        public Customer? XmlEssentials_ReadCustomer_FirstCall()
        {
            return EssentialsXmlSerializer.Deserialize<Customer>(_xml, _firstOptions);
        }

        /// <summary>
        /// Source-generated context read.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadCustomer")]
        public Customer? XmlEssentials_ReadCustomer_SourceGen()
        {
            return EssentialsXmlSerializer.Deserialize(_xmlGenerated, GraphXmlContext.Default.Customer);
        }

        /// <summary>
        /// Cached XmlEssentials reflection write.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteCustomer")]
        public long XmlEssentials_WriteCustomer()
        {
            _buffer.ResetWrittenCount();
            EssentialsXmlSerializer.Serialize(_buffer, _value);
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// First-call XmlEssentials reflection write: type-info bind plus serialize.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteCustomer")]
        public long XmlEssentials_WriteCustomer_FirstCall()
        {
            _buffer.ResetWrittenCount();
            EssentialsXmlSerializer.Serialize(_buffer, _value, _firstOptions);
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// Source-generated context write.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteCustomer")]
        public long XmlEssentials_WriteCustomer_SourceGen()
        {
            _buffer.ResetWrittenCount();
            EssentialsXmlSerializer.Serialize(_buffer, _value, GraphXmlContext.Default.Customer);
            return _buffer.WrittenCount;
        }

        #endregion

    }

}
