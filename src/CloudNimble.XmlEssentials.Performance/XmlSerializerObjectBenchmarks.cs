using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.XmlEssentials.Performance.Models;
using BclXmlSerializer = System.Xml.Serialization.XmlSerializer;
using EssentialsXmlSerializer = CloudNimble.Text.Xml.XmlSerializer;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Compares <see cref="EssentialsXmlSerializer"/> to a cached BCL <see cref="BclXmlSerializer"/> on objects,
    /// with <see cref="JsonSerializer"/> as a sidecar.
    /// </summary>
    /// <remarks>
    /// Each library reads the document it wrote. Every write goes to a sink that is reused between calls, so the
    /// rows measure the serializer and not the copy out. The BCL serializer cannot map a record with a constructor
    /// or a dictionary, so those categories have no BCL row.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlSerializerObjectBenchmarks
    {

        #region Fields

        private const int Count = 1_000;
        private MemoryStream _bclOutput = null!;
        private XmlReaderSettings _bclReaderSettings = null!;
        private XmlWriterSettings _bclWriterSettings = null!;
        private ArrayBufferWriter<byte> _buffer = null!;
        private ArrayBufferWriter<byte> _jsonBuffer = null!;
        private Utf8JsonWriter _jsonWriter = null!;
        private BclXmlSerializer _customerBcl = null!;
        private byte[] _customerBclXml = null!;
        private byte[] _customerJson = null!;
        private Customer _customerValue = null!;
        private byte[] _customerXml = null!;
        private byte[] _dictionaryJson = null!;
        private Dictionary<string, int> _dictionaryValue = null!;
        private byte[] _dictionaryXml = null!;
        private BclXmlSerializer _drawingBcl = null!;
        private byte[] _drawingBclXml = null!;
        private byte[] _drawingJson = null!;
        private Drawing _drawingValue = null!;
        private byte[] _drawingXml = null!;
        private byte[] _membersJson = null!;
        private List<Member> _membersValue = null!;
        private byte[] _membersXml = null!;
        private BclXmlSerializer _moneyBcl = null!;
        private byte[] _moneyBclXml = null!;
        private byte[] _moneyJson = null!;
        private List<Money> _moneyValue = null!;
        private byte[] _moneyXml = null!;
        private BclXmlSerializer _orderBcl = null!;
        private byte[] _orderBclXml = null!;
        private byte[] _orderJson = null!;
        private Order _orderValue = null!;
        private byte[] _orderXml = null!;
        private BclXmlSerializer _ordersBcl = null!;
        private byte[] _ordersBclXml = null!;
        private byte[] _ordersJson = null!;
        private List<Order> _ordersValue = null!;
        private byte[] _ordersXml = null!;
        private BclXmlSerializer _readingsBcl = null!;
        private byte[] _readingsBclXml = null!;
        private byte[] _readingsJson = null!;
        private List<Reading> _readingsValue = null!;
        private byte[] _readingsXml = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// BCL read of a nested object with two child objects and two lists.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadCustomer")]
        public Customer? Bcl_ReadCustomer()
        {
            return (Customer?)ReadBcl(_customerBcl, _customerBclXml);
        }

        /// <summary>
        /// BCL read of a list of 1,000 objects of three runtime types.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadDrawing")]
        public Drawing? Bcl_ReadDrawing()
        {
            return (Drawing?)ReadBcl(_drawingBcl, _drawingBclXml);
        }

        /// <summary>
        /// BCL read of a list of 1,000 elements with simple content and one attribute.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadMoney")]
        public List<Money>? Bcl_ReadMoney()
        {
            return (List<Money>?)ReadBcl(_moneyBcl, _moneyBclXml);
        }

        /// <summary>
        /// BCL read of one flat object of nine scalars.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadOrder")]
        public Order? Bcl_ReadOrder()
        {
            return (Order?)ReadBcl(_orderBcl, _orderBclXml);
        }

        /// <summary>
        /// BCL read of a list of 1,000 flat objects.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadOrders")]
        public List<Order>? Bcl_ReadOrders()
        {
            return (List<Order>?)ReadBcl(_ordersBcl, _ordersBclXml);
        }

        /// <summary>
        /// BCL read of a list of 1,000 objects mapped as attributes.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadReadings")]
        public List<Reading>? Bcl_ReadReadings()
        {
            return (List<Reading>?)ReadBcl(_readingsBcl, _readingsBclXml);
        }

        /// <summary>
        /// BCL write of a nested object with two child objects and two lists.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteCustomer")]
        public long Bcl_WriteCustomer()
        {
            return WriteBcl(_customerBcl, _customerValue);
        }

        /// <summary>
        /// BCL write of a list of 1,000 objects of three runtime types.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteDrawing")]
        public long Bcl_WriteDrawing()
        {
            return WriteBcl(_drawingBcl, _drawingValue);
        }

        /// <summary>
        /// BCL write of a list of 1,000 elements with simple content and one attribute.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteMoney")]
        public long Bcl_WriteMoney()
        {
            return WriteBcl(_moneyBcl, _moneyValue);
        }

        /// <summary>
        /// BCL write of one flat object of nine scalars.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteOrder")]
        public long Bcl_WriteOrder()
        {
            return WriteBcl(_orderBcl, _orderValue);
        }

        /// <summary>
        /// BCL write of a list of 1,000 flat objects.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteOrders")]
        public long Bcl_WriteOrders()
        {
            return WriteBcl(_ordersBcl, _ordersValue);
        }

        /// <summary>
        /// BCL write of a list of 1,000 objects mapped as attributes.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteReadings")]
        public long Bcl_WriteReadings()
        {
            return WriteBcl(_readingsBcl, _readingsValue);
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
        /// Builds serializers, values, and payloads once.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _bclOutput = new MemoryStream(262_144);
            _bclReaderSettings = new XmlReaderSettings { CloseInput = false };
            _bclWriterSettings = new XmlWriterSettings { CloseOutput = false, Encoding = new UTF8Encoding(false) };
            _buffer = new ArrayBufferWriter<byte>(262_144);
            _jsonBuffer = new ArrayBufferWriter<byte>(262_144);
            _jsonWriter = new Utf8JsonWriter(_jsonBuffer);
            var placed = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            _customerValue = new Customer
            {
                Billing = new Address { City = "Springfield", PostalCode = "49007", Street = "742 Evergreen Terrace" },
                Lines = [.. Enumerable.Range(0, 20).Select(i => new OrderLine { Price = 9.99m + i, Quantity = i + 1, Sku = $"SKU-{i:D5}" })],
                Name = "Homer & Marge <Simpson>",
                Shipping = new Address { City = "Shelbyville", PostalCode = "49008", Street = "1 Main Street" },
                Tags = ["retail", "priority", "net-30", "tax-exempt"],
            };
            _dictionaryValue = Enumerable.Range(0, 100).ToDictionary(i => $"Key{i}", i => i * 7);
            _drawingValue = new Drawing
            {
                Shapes = [.. Enumerable.Range(0, Count).Select(i => (i % 3) switch
                {
                    0 => new Circle { Name = $"circle {i}", Radius = i * 0.5 },
                    1 => new Square { Name = $"square {i}", Side = i * 0.25 },
                    _ => new Shape { Name = $"shape {i}" },
                })],
            };
            _membersValue = [.. Enumerable.Range(0, Count).Select(i => new Member($"Member {i}", 20 + (i % 50), "Kalamazoo", i % 2 == 0))];
            _moneyValue = [.. Enumerable.Range(0, Count).Select(i => new Money { Amount = 100.25m + i, Currency = i % 2 == 0 ? "USD" : "EUR" })];
            _ordersValue = [.. Enumerable.Range(0, Count).Select(i => new Order
            {
                Customer = $"Customer {i}",
                Id = i,
                Notes = "Leave at the back door & ring twice",
                Placed = placed.AddMinutes(i),
                Sequence = 9_000_000_000L + i,
                Shipped = i % 2 == 0,
                Status = (OrderStatus)(i % 3),
                Total = 1234.56m + i,
                Weight = 12.5 + i,
            })];
            _orderValue = _ordersValue[7];
            _readingsValue = [.. Enumerable.Range(0, Count).Select(i => new Reading { At = placed.AddSeconds(i), Id = i, Sensor = $"sensor-{i % 16}", Value = 20.5 + (i * 0.125) })];
            _customerBcl = new BclXmlSerializer(typeof(Customer));
            _drawingBcl = new BclXmlSerializer(typeof(Drawing));
            _moneyBcl = new BclXmlSerializer(typeof(List<Money>));
            _orderBcl = new BclXmlSerializer(typeof(Order));
            _ordersBcl = new BclXmlSerializer(typeof(List<Order>));
            _readingsBcl = new BclXmlSerializer(typeof(List<Reading>));
            _customerBclXml = BclBytes(_customerBcl, _customerValue);
            _drawingBclXml = BclBytes(_drawingBcl, _drawingValue);
            _moneyBclXml = BclBytes(_moneyBcl, _moneyValue);
            _orderBclXml = BclBytes(_orderBcl, _orderValue);
            _ordersBclXml = BclBytes(_ordersBcl, _ordersValue);
            _readingsBclXml = BclBytes(_readingsBcl, _readingsValue);
            _customerJson = JsonSerializer.SerializeToUtf8Bytes(_customerValue);
            _dictionaryJson = JsonSerializer.SerializeToUtf8Bytes(_dictionaryValue);
            _drawingJson = JsonSerializer.SerializeToUtf8Bytes(_drawingValue);
            _membersJson = JsonSerializer.SerializeToUtf8Bytes(_membersValue);
            _moneyJson = JsonSerializer.SerializeToUtf8Bytes(_moneyValue);
            _orderJson = JsonSerializer.SerializeToUtf8Bytes(_orderValue);
            _ordersJson = JsonSerializer.SerializeToUtf8Bytes(_ordersValue);
            _readingsJson = JsonSerializer.SerializeToUtf8Bytes(_readingsValue);
            _customerXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_customerValue);
            _dictionaryXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_dictionaryValue);
            _drawingXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_drawingValue);
            _membersXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_membersValue);
            _moneyXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_moneyValue);
            _orderXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_orderValue);
            _ordersXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_ordersValue);
            _readingsXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_readingsValue);
        }

        /// <summary>
        /// System.Text.Json read of a nested object with two child objects and two lists.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadCustomer")]
        public Customer? Stj_ReadCustomer()
        {
            return JsonSerializer.Deserialize<Customer>(_customerJson);
        }

        /// <summary>
        /// System.Text.Json read of a dictionary of 100 integers keyed by name.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadDictionary")]
        public Dictionary<string, int>? Stj_ReadDictionary()
        {
            return JsonSerializer.Deserialize<Dictionary<string, int>>(_dictionaryJson);
        }

        /// <summary>
        /// System.Text.Json read of a list of 1,000 objects of three runtime types.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadDrawing")]
        public Drawing? Stj_ReadDrawing()
        {
            return JsonSerializer.Deserialize<Drawing>(_drawingJson);
        }

        /// <summary>
        /// System.Text.Json read of a list of 1,000 records created through a constructor.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadMembers")]
        public List<Member>? Stj_ReadMembers()
        {
            return JsonSerializer.Deserialize<List<Member>>(_membersJson);
        }

        /// <summary>
        /// System.Text.Json read of a list of 1,000 elements with simple content and one attribute.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadMoney")]
        public List<Money>? Stj_ReadMoney()
        {
            return JsonSerializer.Deserialize<List<Money>>(_moneyJson);
        }

        /// <summary>
        /// System.Text.Json read of one flat object of nine scalars.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadOrder")]
        public Order? Stj_ReadOrder()
        {
            return JsonSerializer.Deserialize<Order>(_orderJson);
        }

        /// <summary>
        /// System.Text.Json read of a list of 1,000 flat objects.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadOrders")]
        public List<Order>? Stj_ReadOrders()
        {
            return JsonSerializer.Deserialize<List<Order>>(_ordersJson);
        }

        /// <summary>
        /// System.Text.Json read of a list of 1,000 objects mapped as attributes.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadReadings")]
        public List<Reading>? Stj_ReadReadings()
        {
            return JsonSerializer.Deserialize<List<Reading>>(_readingsJson);
        }

        /// <summary>
        /// System.Text.Json write of a nested object with two child objects and two lists.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteCustomer")]
        public long Stj_WriteCustomer()
        {
            return WriteJson(_customerValue);
        }

        /// <summary>
        /// System.Text.Json write of a dictionary of 100 integers keyed by name.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteDictionary")]
        public long Stj_WriteDictionary()
        {
            return WriteJson(_dictionaryValue);
        }

        /// <summary>
        /// System.Text.Json write of a list of 1,000 objects of three runtime types.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteDrawing")]
        public long Stj_WriteDrawing()
        {
            return WriteJson(_drawingValue);
        }

        /// <summary>
        /// System.Text.Json write of a list of 1,000 records created through a constructor.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteMembers")]
        public long Stj_WriteMembers()
        {
            return WriteJson(_membersValue);
        }

        /// <summary>
        /// System.Text.Json write of a list of 1,000 elements with simple content and one attribute.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteMoney")]
        public long Stj_WriteMoney()
        {
            return WriteJson(_moneyValue);
        }

        /// <summary>
        /// System.Text.Json write of one flat object of nine scalars.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteOrder")]
        public long Stj_WriteOrder()
        {
            return WriteJson(_orderValue);
        }

        /// <summary>
        /// System.Text.Json write of a list of 1,000 flat objects.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteOrders")]
        public long Stj_WriteOrders()
        {
            return WriteJson(_ordersValue);
        }

        /// <summary>
        /// System.Text.Json write of a list of 1,000 objects mapped as attributes.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteReadings")]
        public long Stj_WriteReadings()
        {
            return WriteJson(_readingsValue);
        }

        /// <summary>
        /// XmlEssentials read of a nested object with two child objects and two lists.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadCustomer")]
        public Customer? XmlEssentials_ReadCustomer()
        {
            return EssentialsXmlSerializer.Deserialize<Customer>(_customerXml);
        }

        /// <summary>
        /// XmlEssentials read of a dictionary of 100 integers keyed by name.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadDictionary")]
        public Dictionary<string, int>? XmlEssentials_ReadDictionary()
        {
            return EssentialsXmlSerializer.Deserialize<Dictionary<string, int>>(_dictionaryXml);
        }

        /// <summary>
        /// XmlEssentials read of a list of 1,000 objects of three runtime types.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadDrawing")]
        public Drawing? XmlEssentials_ReadDrawing()
        {
            return EssentialsXmlSerializer.Deserialize<Drawing>(_drawingXml);
        }

        /// <summary>
        /// XmlEssentials read of a list of 1,000 records created through a constructor.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadMembers")]
        public List<Member>? XmlEssentials_ReadMembers()
        {
            return EssentialsXmlSerializer.Deserialize<List<Member>>(_membersXml);
        }

        /// <summary>
        /// XmlEssentials read of a list of 1,000 elements with simple content and one attribute.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadMoney")]
        public List<Money>? XmlEssentials_ReadMoney()
        {
            return EssentialsXmlSerializer.Deserialize<List<Money>>(_moneyXml);
        }

        /// <summary>
        /// XmlEssentials read of one flat object of nine scalars.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadOrder")]
        public Order? XmlEssentials_ReadOrder()
        {
            return EssentialsXmlSerializer.Deserialize<Order>(_orderXml);
        }

        /// <summary>
        /// XmlEssentials read of a list of 1,000 flat objects.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadOrders")]
        public List<Order>? XmlEssentials_ReadOrders()
        {
            return EssentialsXmlSerializer.Deserialize<List<Order>>(_ordersXml);
        }

        /// <summary>
        /// XmlEssentials read of a list of 1,000 objects mapped as attributes.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadReadings")]
        public List<Reading>? XmlEssentials_ReadReadings()
        {
            return EssentialsXmlSerializer.Deserialize<List<Reading>>(_readingsXml);
        }

        /// <summary>
        /// XmlEssentials write of a nested object with two child objects and two lists.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteCustomer")]
        public long XmlEssentials_WriteCustomer()
        {
            return WriteXml(_customerValue);
        }

        /// <summary>
        /// XmlEssentials write of a dictionary of 100 integers keyed by name.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteDictionary")]
        public long XmlEssentials_WriteDictionary()
        {
            return WriteXml(_dictionaryValue);
        }

        /// <summary>
        /// XmlEssentials write of a list of 1,000 objects of three runtime types.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteDrawing")]
        public long XmlEssentials_WriteDrawing()
        {
            return WriteXml(_drawingValue);
        }

        /// <summary>
        /// XmlEssentials write of a list of 1,000 records created through a constructor.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteMembers")]
        public long XmlEssentials_WriteMembers()
        {
            return WriteXml(_membersValue);
        }

        /// <summary>
        /// XmlEssentials write of a list of 1,000 elements with simple content and one attribute.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteMoney")]
        public long XmlEssentials_WriteMoney()
        {
            return WriteXml(_moneyValue);
        }

        /// <summary>
        /// XmlEssentials write of one flat object of nine scalars.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteOrder")]
        public long XmlEssentials_WriteOrder()
        {
            return WriteXml(_orderValue);
        }

        /// <summary>
        /// XmlEssentials write of a list of 1,000 flat objects.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteOrders")]
        public long XmlEssentials_WriteOrders()
        {
            return WriteXml(_ordersValue);
        }

        /// <summary>
        /// XmlEssentials write of a list of 1,000 objects mapped as attributes.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteReadings")]
        public long XmlEssentials_WriteReadings()
        {
            return WriteXml(_readingsValue);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Writes <paramref name="value"/> with a cached BCL serializer and copies the document out.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="value">The value.</param>
        /// <returns>The document.</returns>
        private byte[] BclBytes(BclXmlSerializer serializer, object value)
        {
            WriteBcl(serializer, value);
            return _bclOutput.ToArray();
        }

        /// <summary>
        /// Reads <paramref name="xml"/> with a cached BCL serializer.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="xml">The document.</param>
        /// <returns>The value.</returns>
        private object? ReadBcl(BclXmlSerializer serializer, byte[] xml)
        {
            using var stream = new MemoryStream(xml, writable: false);
            using var reader = XmlReader.Create(stream, _bclReaderSettings);
            return serializer.Deserialize(reader);
        }

        /// <summary>
        /// Writes <paramref name="value"/> with a cached BCL serializer to the reused stream.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="value">The value.</param>
        /// <returns>The document length.</returns>
        private long WriteBcl(BclXmlSerializer serializer, object value)
        {
            _bclOutput.SetLength(0);
            using (var writer = XmlWriter.Create(_bclOutput, _bclWriterSettings))
            {
                serializer.Serialize(writer, value);
            }

            return _bclOutput.Length;
        }

        /// <summary>
        /// Writes <paramref name="value"/> with System.Text.Json to the reused buffer.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The document length.</returns>
        private long WriteJson<T>(T value)
        {
            _jsonBuffer.ResetWrittenCount();
            _jsonWriter.Reset(_jsonBuffer);
            JsonSerializer.Serialize(_jsonWriter, value);
            return _jsonBuffer.WrittenCount;
        }

        /// <summary>
        /// Writes <paramref name="value"/> with XmlEssentials to the reused buffer.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns>The document length.</returns>
        private long WriteXml<T>(T value)
        {
            _buffer.ResetWrittenCount();
            EssentialsXmlSerializer.Serialize(_buffer, value);
            return _buffer.WrittenCount;
        }

        #endregion

    }

}
