using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Performance.Models;
using EssentialsXmlSerializer = CloudNimble.Text.Xml.XmlSerializer;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Measures a stream of objects read and written one at a time, where
    /// <see cref="XmlSerializerSequenceBenchmarks"/> streams numbers. An object is read only once all of it has
    /// arrived, so each one is looked ahead for and then read.
    /// </summary>
    /// <remarks>
    /// System.Text.Json is beside it on the same values. <c>System.Xml.Serialization</c> has no counterpart.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlSerializerObjectSequenceBenchmarks
    {

        #region Fields

        private const int Count = 1_000;
        private MemoryStream _jsonInput = null!;
        private MemoryStream _output = null!;
        private XmlTypeInfo<Order> _typeInfo = null!;
        private Order[] _values = null!;
        private MemoryStream _xmlInput = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// Disposes the streams.
        /// </summary>
        [GlobalCleanup]
        public void Cleanup()
        {
            _jsonInput.Dispose();
            _output.Dispose();
            _xmlInput.Dispose();
        }

        /// <summary>
        /// Builds the orders and the documents that hold them.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            var placed = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            _values = [.. Enumerable.Range(0, Count).Select(i => new Order
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
            _typeInfo = XmlSerializerOptions.Default.GetTypeInfo<Order>();
            _jsonInput = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(_values));
            _output = new MemoryStream(262_144);
            XmlEssentials_WriteOrderSequence().GetAwaiter().GetResult();
            _xmlInput = new MemoryStream(_output.ToArray());
        }

        /// <summary>
        /// System.Text.Json: 1,000 orders read from a stream one at a time.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadOrderSequence")]
        public async Task<long> Stj_ReadOrderSequence()
        {
            _jsonInput.Position = 0;
            var sum = 0L;
            await foreach (var value in JsonSerializer.DeserializeAsyncEnumerable<Order>(_jsonInput))
            {
                sum += value!.Id;
            }

            return sum;
        }

        /// <summary>
        /// System.Text.Json: 1,000 orders written to a stream one at a time.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteOrderSequence")]
        public async Task<long> Stj_WriteOrderSequence()
        {
            _output.SetLength(0);
            await JsonSerializer.SerializeAsync(_output, Values());
            return _output.Length;
        }

        /// <summary>
        /// XmlEssentials: 1,000 orders read from a stream one at a time.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ReadOrderSequence")]
        public async Task<long> XmlEssentials_ReadOrderSequence()
        {
            _xmlInput.Position = 0;
            var sum = 0L;
            await foreach (var value in EssentialsXmlSerializer.DeserializeAsyncEnumerable(_xmlInput, _typeInfo))
            {
                sum += value!.Id;
            }

            return sum;
        }

        /// <summary>
        /// XmlEssentials: 1,000 orders written to a stream one at a time.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("WriteOrderSequence")]
        public async Task<long> XmlEssentials_WriteOrderSequence()
        {
            _output.SetLength(0);
            await EssentialsXmlSerializer.SerializeAsyncEnumerable(_output, Values(), _typeInfo);
            return _output.Length;
        }

        #endregion

        #region Private Methods

        private async IAsyncEnumerable<Order> Values()
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
