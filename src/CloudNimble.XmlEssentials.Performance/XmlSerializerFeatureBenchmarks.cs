using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Performance.Models;
using BclXmlSerializer = System.Xml.Serialization.XmlSerializer;
using BclXmlSerializerNamespaces = System.Xml.Serialization.XmlSerializerNamespaces;
using EssentialsXmlSerializer = CloudNimble.Text.Xml.XmlSerializer;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Measures features of <see cref="EssentialsXmlSerializer"/> that <see cref="XmlSerializerObjectBenchmarks"/>
    /// does not reach: a document in several namespaces with chosen prefixes, reference handling, derived types
    /// told apart by an attribute and by <c>xsi:type</c>, and the options that write what
    /// <c>System.Xml.Serialization</c> writes.
    /// </summary>
    /// <remarks>
    /// The BCL <see cref="BclXmlSerializer"/> is the baseline wherever it has the feature. Reference handling has
    /// no BCL counterpart, so there the plain XmlEssentials row is the baseline and each ratio is the price of the
    /// handler. System.Text.Json is beside each category it has a counterpart for.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlSerializerFeatureBenchmarks
    {

        #region Fields

        private const int Count = 1_000;
        private MemoryStream _bclOutput = null!;
        private XmlReaderSettings _bclReaderSettings = null!;
        private XmlWriterSettings _bclWriterSettings = null!;
        private ArrayBufferWriter<byte> _buffer = null!;
        private BclXmlSerializer _customerBcl = null!;
        private byte[] _customerBclXml = null!;
        private byte[] _customerPresetXml = null!;
        private Customer _customerValue = null!;
        private byte[] _customersJson = null!;
        private byte[] _customersPreservedJson = null!;
        private byte[] _customersPreservedXml = null!;
        private List<Customer> _customersValue = null!;
        private byte[] _customersXml = null!;
        private BclXmlSerializer _feedBcl = null!;
        private byte[] _feedBclXml = null!;
        private BclXmlSerializerNamespaces _feedBclNamespaces = null!;
        private XmlSerializerOptions _feedOptions = null!;
        private Feed _feedValue = null!;
        private byte[] _feedXml = null!;
        private XmlSerializerOptions _ignoreCycles = null!;
        private ArrayBufferWriter<byte> _jsonBuffer = null!;
        private JsonSerializerOptions _jsonIgnoreCycles = null!;
        private JsonSerializerOptions _jsonPreserve = null!;
        private Utf8JsonWriter _jsonWriter = null!;
        private XmlSerializerOptions _preserve = null!;
        private byte[] _taggedJson = null!;
        private TaggedDrawing _taggedValue = null!;
        private byte[] _taggedXml = null!;
        private BclXmlSerializer _typedBcl = null!;
        private byte[] _typedBclXml = null!;
        private TypedDrawing _typedValue = null!;
        private byte[] _typedXml = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// BCL: one customer graph read.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadCustomerBclPreset")]
        public Customer? Bcl_ReadCustomer()
        {
            return (Customer?)ReadBcl(_customerBcl, _customerBclXml);
        }

        /// <summary>
        /// BCL: a feed of 200 episodes in four namespaces read.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadFeed")]
        public Feed? Bcl_ReadFeed()
        {
            return (Feed?)ReadBcl(_feedBcl, _feedBclXml);
        }

        /// <summary>
        /// BCL: 1,000 shapes of three types read, told apart by <c>xsi:type</c>.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadTypedDrawing")]
        public TypedDrawing? Bcl_ReadTypedDrawing()
        {
            return (TypedDrawing?)ReadBcl(_typedBcl, _typedBclXml);
        }

        /// <summary>
        /// BCL: one customer graph written as <c>Serialize(Stream, object)</c> writes it, indented, which is the
        /// document <see cref="XmlSerializerOptions.Bcl"/> writes.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteCustomerBclPreset")]
        public long Bcl_WriteCustomer()
        {
            _bclOutput.SetLength(0);
            _customerBcl.Serialize(_bclOutput, _customerValue);
            return _bclOutput.Length;
        }

        /// <summary>
        /// BCL: a feed of 200 episodes written with its prefixes.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteFeed")]
        public long Bcl_WriteFeed()
        {
            return WriteBcl(_feedBcl, _feedValue, _feedBclNamespaces);
        }

        /// <summary>
        /// BCL: 1,000 shapes of three types written, each derived one with <c>xsi:type</c>.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteTypedDrawing")]
        public long Bcl_WriteTypedDrawing()
        {
            return WriteBcl(_typedBcl, _typedValue, null);
        }

        /// <summary>
        /// Disposes reusable buffers.
        /// </summary>
        [GlobalCleanup]
        public void Cleanup()
        {
            _bclOutput.Dispose();
            _jsonWriter.Dispose();
        }

        /// <summary>
        /// Builds the values, the cached BCL serializers, the options, and the documents that are read.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _bclOutput = new MemoryStream(1_048_576);
            _bclReaderSettings = new XmlReaderSettings { CloseInput = false };
            _bclWriterSettings = new XmlWriterSettings { CloseOutput = false, Encoding = new UTF8Encoding(false) };
            _buffer = new ArrayBufferWriter<byte>(1_048_576);
            _jsonBuffer = new ArrayBufferWriter<byte>(1_048_576);
            _jsonWriter = new Utf8JsonWriter(_jsonBuffer);

            // The feed: elements in no namespace beside elements of the same local name in another.
            _feedValue = new Feed
            {
                Channel = new FeedChannel
                {
                    AtomLink = new FeedLink { Href = "https://feeds.example.com/show", Rel = "self" },
                    Author = "The Example Show",
                    Items = [.. Enumerable.Range(0, 200).Select(i => new FeedItem
                    {
                        Description = $"Episode {i}: what we said & what we meant.",
                        Duration = 1_800 + i,
                        Encoded = $"<p>Episode {i}</p><p>Notes, links & credits for the episode.</p>",
                        Episode = i,
                        Guid = $"7c9e6679-7425-40de-944b-e07fc1f9{i:D4}",
                        ITunesTitle = $"What we said ({i})",
                        Link = $"https://example.com/episodes/{i}",
                        Published = "Tue, 24 Nov 2026 08:00:00 +0000",
                        Title = $"Episode {i}: What we said",
                    })],
                    Link = "https://example.com/",
                    Title = "The Example Show",
                },
                Version = "2.0",
            };
            _feedOptions = new XmlSerializerOptions
            {
                Namespaces = new Dictionary<string, string> { ["atom"] = Feed.Atom, ["content"] = Feed.Content, ["itunes"] = Feed.ITunes },
            };
            _feedBclNamespaces = new BclXmlSerializerNamespaces();
            _feedBclNamespaces.Add("atom", Feed.Atom);
            _feedBclNamespaces.Add("content", Feed.Content);
            _feedBclNamespaces.Add("itunes", Feed.ITunes);
            _feedBcl = new BclXmlSerializer(typeof(Feed));
            WriteBcl(_feedBcl, _feedValue, _feedBclNamespaces);
            _feedBclXml = _bclOutput.ToArray();
            _feedXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_feedValue, _feedOptions);

            // 200 customers that share addresses: the two addresses of a customer are one object, and one
            // warehouse address is every customer's.
            var warehouse = new Address { City = "Shelbyville", PostalCode = "49008", Street = "1 Main Street" };
            _customersValue = [.. Enumerable.Range(0, 200).Select(i =>
            {
                var home = new Address { City = "Springfield", PostalCode = "49007", Street = $"{i} Evergreen Terrace" };
                return new Customer
                {
                    Billing = i % 2 == 0 ? home : warehouse,
                    Lines = [.. Enumerable.Range(0, 5).Select(line => new OrderLine { Price = 9.99m + line, Quantity = line + 1, Sku = $"SKU-{line:D5}" })],
                    Name = $"Customer {i}",
                    Shipping = home,
                    Tags = ["retail", "priority"],
                };
            })];
            _customerValue = _customersValue[0];
            _preserve = new XmlSerializerOptions { ReferenceHandler = XmlReferenceHandler.Preserve };
            _ignoreCycles = new XmlSerializerOptions { ReferenceHandler = XmlReferenceHandler.IgnoreCycles };
            _jsonPreserve = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };
            _jsonIgnoreCycles = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles };
            _customersJson = JsonSerializer.SerializeToUtf8Bytes(_customersValue);
            _customersPreservedJson = JsonSerializer.SerializeToUtf8Bytes(_customersValue, _jsonPreserve);
            _customersXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_customersValue);
            _customersPreservedXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_customersValue, _preserve);

            _customerBcl = new BclXmlSerializer(typeof(Customer));
            Bcl_WriteCustomer();
            _customerBclXml = _bclOutput.ToArray();
            _customerPresetXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_customerValue, XmlSerializerOptions.Bcl);

            _taggedValue = new TaggedDrawing
            {
                Shapes = [.. Enumerable.Range(0, Count).Select(i => (i % 3) switch
                {
                    0 => new TaggedCircle { Name = $"circle {i}", Radius = i * 0.5 },
                    1 => new TaggedSquare { Name = $"square {i}", Side = i * 0.25 },
                    _ => new TaggedShape { Name = $"shape {i}" },
                })],
            };
            _taggedJson = JsonSerializer.SerializeToUtf8Bytes(_taggedValue);
            _taggedXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_taggedValue);

            _typedValue = new TypedDrawing
            {
                Shapes = [.. Enumerable.Range(0, Count).Select(i => (i % 3) switch
                {
                    0 => new TypedCircle { Name = $"circle {i}", Radius = i * 0.5 },
                    1 => new TypedSquare { Name = $"square {i}", Side = i * 0.25 },
                    _ => new TypedShape { Name = $"shape {i}" },
                })],
            };
            _typedBcl = new BclXmlSerializer(typeof(TypedDrawing));
            WriteBcl(_typedBcl, _typedValue, null);
            _typedBclXml = _bclOutput.ToArray();
            _typedXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_typedValue);
        }

        /// <summary>
        /// System.Text.Json: 200 customers read.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadCustomers")]
        public List<Customer>? Stj_ReadCustomers()
        {
            return JsonSerializer.Deserialize<List<Customer>>(_customersJson);
        }

        /// <summary>
        /// System.Text.Json: 200 customers read with references preserved.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadCustomers")]
        public List<Customer>? Stj_ReadCustomers_Preserve()
        {
            return JsonSerializer.Deserialize<List<Customer>>(_customersPreservedJson, _jsonPreserve);
        }

        /// <summary>
        /// System.Text.Json: 1,000 shapes of three types read, told apart by a <c>kind</c> property.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadTaggedDrawing")]
        public TaggedDrawing? Stj_ReadTaggedDrawing()
        {
            return JsonSerializer.Deserialize<TaggedDrawing>(_taggedJson);
        }

        /// <summary>
        /// System.Text.Json: 200 customers written.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomers")]
        public long Stj_WriteCustomers()
        {
            return WriteJson(_customersValue, null);
        }

        /// <summary>
        /// System.Text.Json: 200 customers written with cycles ignored.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomers")]
        public long Stj_WriteCustomers_IgnoreCycles()
        {
            return WriteJson(_customersValue, _jsonIgnoreCycles);
        }

        /// <summary>
        /// System.Text.Json: 200 customers written with references preserved.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomers")]
        public long Stj_WriteCustomers_Preserve()
        {
            return WriteJson(_customersValue, _jsonPreserve);
        }

        /// <summary>
        /// System.Text.Json: 1,000 shapes of three types written, each derived one with a <c>kind</c> property.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteTaggedDrawing")]
        public long Stj_WriteTaggedDrawing()
        {
            return WriteJson(_taggedValue, null);
        }

        /// <summary>
        /// XmlEssentials: one customer graph read with <see cref="XmlSerializerOptions.Bcl"/>.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadCustomerBclPreset")]
        public Customer? XmlEssentials_ReadCustomer_BclPreset()
        {
            return EssentialsXmlSerializer.Deserialize<Customer>(_customerPresetXml, XmlSerializerOptions.Bcl);
        }

        /// <summary>
        /// XmlEssentials: 200 customers read, the baseline for the price of a reference handler.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadCustomers")]
        public List<Customer>? XmlEssentials_ReadCustomers()
        {
            return EssentialsXmlSerializer.Deserialize<List<Customer>>(_customersXml);
        }

        /// <summary>
        /// XmlEssentials: 200 customers read with references preserved.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadCustomers")]
        public List<Customer>? XmlEssentials_ReadCustomers_Preserve()
        {
            return EssentialsXmlSerializer.Deserialize<List<Customer>>(_customersPreservedXml, _preserve);
        }

        /// <summary>
        /// XmlEssentials: a feed of 200 episodes in four namespaces read.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadFeed")]
        public Feed? XmlEssentials_ReadFeed()
        {
            return EssentialsXmlSerializer.Deserialize<Feed>(_feedXml, _feedOptions);
        }

        /// <summary>
        /// XmlEssentials: 1,000 shapes of three types read, told apart by a <c>kind</c> attribute.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadTaggedDrawing")]
        public TaggedDrawing? XmlEssentials_ReadTaggedDrawing()
        {
            return EssentialsXmlSerializer.Deserialize<TaggedDrawing>(_taggedXml);
        }

        /// <summary>
        /// XmlEssentials: 1,000 shapes of three types read, told apart by <c>xsi:type</c>.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadTypedDrawing")]
        public TypedDrawing? XmlEssentials_ReadTypedDrawing()
        {
            return EssentialsXmlSerializer.Deserialize<TypedDrawing>(_typedXml);
        }

        /// <summary>
        /// XmlEssentials: one customer graph written with <see cref="XmlSerializerOptions.Bcl"/>.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomerBclPreset")]
        public long XmlEssentials_WriteCustomer_BclPreset()
        {
            return WriteXml(_customerValue, XmlSerializerOptions.Bcl);
        }

        /// <summary>
        /// XmlEssentials: 200 customers written, the baseline for the price of a reference handler.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteCustomers")]
        public long XmlEssentials_WriteCustomers()
        {
            return WriteXml(_customersValue, null);
        }

        /// <summary>
        /// XmlEssentials: 200 customers written with cycles ignored.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomers")]
        public long XmlEssentials_WriteCustomers_IgnoreCycles()
        {
            return WriteXml(_customersValue, _ignoreCycles);
        }

        /// <summary>
        /// XmlEssentials: 200 customers written with references preserved.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteCustomers")]
        public long XmlEssentials_WriteCustomers_Preserve()
        {
            return WriteXml(_customersValue, _preserve);
        }

        /// <summary>
        /// XmlEssentials: a feed of 200 episodes written with its prefixes.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteFeed")]
        public long XmlEssentials_WriteFeed()
        {
            return WriteXml(_feedValue, _feedOptions);
        }

        /// <summary>
        /// XmlEssentials: 1,000 shapes of three types written, each derived one with a <c>kind</c> attribute.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteTaggedDrawing")]
        public long XmlEssentials_WriteTaggedDrawing()
        {
            return WriteXml(_taggedValue, null);
        }

        /// <summary>
        /// XmlEssentials: 1,000 shapes of three types written, each derived one with <c>xsi:type</c>.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteTypedDrawing")]
        public long XmlEssentials_WriteTypedDrawing()
        {
            return WriteXml(_typedValue, null);
        }

        #endregion

        #region Private Methods

        private object? ReadBcl(BclXmlSerializer serializer, byte[] xml)
        {
            using var stream = new MemoryStream(xml, writable: false);
            using var reader = XmlReader.Create(stream, _bclReaderSettings);
            return serializer.Deserialize(reader);
        }

        private long WriteBcl(BclXmlSerializer serializer, object value, BclXmlSerializerNamespaces? namespaces)
        {
            _bclOutput.SetLength(0);
            using (var writer = XmlWriter.Create(_bclOutput, _bclWriterSettings))
            {
                serializer.Serialize(writer, value, namespaces);
            }

            return _bclOutput.Length;
        }

        private long WriteJson<T>(T value, JsonSerializerOptions? options)
        {
            _jsonBuffer.ResetWrittenCount();
            _jsonWriter.Reset(_jsonBuffer);
            JsonSerializer.Serialize(_jsonWriter, value, options);
            return _jsonBuffer.WrittenCount;
        }

        private long WriteXml<T>(T value, XmlSerializerOptions? options)
        {
            _buffer.ResetWrittenCount();
            EssentialsXmlSerializer.Serialize(_buffer, value, options);
            return _buffer.WrittenCount;
        }

        #endregion

    }

}
