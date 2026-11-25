using System.Buffers;
using System.Linq;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Performance.Models;
using EssentialsXmlSerializer = CloudNimble.Text.Xml.XmlSerializer;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Measures each source generation mode on the <see cref="Customer"/> graph: the default (metadata and a write
    /// fast path), metadata only, the write fast path only, and a default context whose options the fast path was
    /// not compiled for, which writes from metadata.
    /// </summary>
    /// <remarks>
    /// The reflection resolver is the baseline, so each ratio says what a generated context changes. System.Text.Json
    /// is beside it with the same three modes.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlSerializerSourceGenBenchmarks
    {

        #region Fields

        private ArrayBufferWriter<byte> _buffer = null!;
        private XmlTypeInfo<Customer> _camelCase = null!;
        private byte[] _json = null!;
        private ArrayBufferWriter<byte> _jsonBuffer = null!;
        private Utf8JsonWriter _jsonWriter = null!;
        private Customer _value = null!;
        private byte[] _xml = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// Disposes the JSON writer.
        /// </summary>
        [GlobalCleanup]
        public void Cleanup()
        {
            _jsonWriter.Dispose();
        }

        /// <summary>
        /// Builds the customer, the documents, and the type info of the context with other options.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
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
            _json = JsonSerializer.SerializeToUtf8Bytes(_value);
            _xml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_value);
            _camelCase = new GraphXmlContext(new XmlSerializerOptions { PropertyNamingPolicy = XmlNamingPolicy.CamelCase }).Customer;
        }

        /// <summary>
        /// System.Text.Json: read through a context in the default mode.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadCustomerSourceGen")]
        public Customer? Stj_Read_Default()
        {
            return JsonSerializer.Deserialize(_json, GraphJsonContext.Default.Customer);
        }

        /// <summary>
        /// System.Text.Json: read through a context that holds metadata only.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadCustomerSourceGen")]
        public Customer? Stj_Read_Metadata()
        {
            return JsonSerializer.Deserialize(_json, GraphMetadataJsonContext.Default.Customer);
        }

        /// <summary>
        /// System.Text.Json: write through a context in the default mode.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomerSourceGen")]
        public long Stj_Write_Default()
        {
            return WriteJson(GraphJsonContext.Default.Customer);
        }

        /// <summary>
        /// System.Text.Json: write through a context that holds metadata only.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomerSourceGen")]
        public long Stj_Write_Metadata()
        {
            return WriteJson(GraphMetadataJsonContext.Default.Customer);
        }

        /// <summary>
        /// System.Text.Json: write through a context that holds the write fast path only.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomerSourceGen")]
        public long Stj_Write_Serialization()
        {
            return WriteJson(GraphSerializationJsonContext.Default.Customer);
        }

        /// <summary>
        /// XmlEssentials: read through a context in the default mode.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadCustomerSourceGen")]
        public Customer? XmlEssentials_Read_Default()
        {
            return EssentialsXmlSerializer.Deserialize(_xml, GraphXmlContext.Default.Customer);
        }

        /// <summary>
        /// XmlEssentials: read through a context that holds metadata only.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadCustomerSourceGen")]
        public Customer? XmlEssentials_Read_Metadata()
        {
            return EssentialsXmlSerializer.Deserialize(_xml, GraphMetadataXmlContext.Default.Customer);
        }

        /// <summary>
        /// XmlEssentials: read through the reflection resolver.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadCustomerSourceGen")]
        public Customer? XmlEssentials_Read_Reflection()
        {
            return EssentialsXmlSerializer.Deserialize<Customer>(_xml);
        }

        /// <summary>
        /// XmlEssentials: write through a context in the default mode, which uses the write fast path.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomerSourceGen")]
        public long XmlEssentials_Write_Default()
        {
            return WriteXml(GraphXmlContext.Default.Customer);
        }

        /// <summary>
        /// XmlEssentials: write through a default context built with a naming policy, which the fast path was not
        /// compiled for, so the engine writes from the metadata.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomerSourceGen")]
        public long XmlEssentials_Write_Default_OtherOptions()
        {
            return WriteXml(_camelCase);
        }

        /// <summary>
        /// XmlEssentials: write through a context that holds metadata only.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomerSourceGen")]
        public long XmlEssentials_Write_Metadata()
        {
            return WriteXml(GraphMetadataXmlContext.Default.Customer);
        }

        /// <summary>
        /// XmlEssentials: write through the reflection resolver.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteCustomerSourceGen")]
        public long XmlEssentials_Write_Reflection()
        {
            _buffer.ResetWrittenCount();
            EssentialsXmlSerializer.Serialize(_buffer, _value);
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials: write through a context that holds the write fast path only.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomerSourceGen")]
        public long XmlEssentials_Write_Serialization()
        {
            return WriteXml(GraphSerializationXmlContext.Default.Customer);
        }

        #endregion

        #region Private Methods

        private long WriteJson(System.Text.Json.Serialization.Metadata.JsonTypeInfo<Customer> typeInfo)
        {
            _jsonBuffer.ResetWrittenCount();
            _jsonWriter.Reset(_jsonBuffer);
            JsonSerializer.Serialize(_jsonWriter, _value, typeInfo);
            return _jsonBuffer.WrittenCount;
        }

        private long WriteXml(XmlTypeInfo<Customer> typeInfo)
        {
            _buffer.ResetWrittenCount();
            EssentialsXmlSerializer.Serialize(_buffer, _value, typeInfo);
            return _buffer.WrittenCount;
        }

        #endregion

    }

}
