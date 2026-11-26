using System.Buffers;
using System.Collections.Generic;
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
    /// Reads and writes 1,000 objects of a value type, whose members the serializer sets in a box.
    /// </summary>
    /// <remarks>
    /// The other object benchmarks hold classes. A value type takes a different path through each member: its
    /// setter changes a boxed value in place, and its getter is handed the value. The rows cover the reflection
    /// resolver, generated metadata, and a field as well as properties.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlSerializerValueTypeBenchmarks
    {

        #region Fields

        private const int Count = 1_000;

        private ArrayBufferWriter<byte> _buffer = null!;
        private XmlTypeInfo<List<Vertex>> _generated = null!;
        private byte[] _json = null!;
        private ArrayBufferWriter<byte> _jsonBuffer = null!;
        private JsonSerializerOptions _jsonOptions = null!;
        private Utf8JsonWriter _jsonWriter = null!;
        private XmlSerializerOptions _options = null!;
        private List<Vertex> _values = null!;
        private byte[] _xml = null!;
        private Utf8XmlWriter _xmlWriter = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// Releases the writers.
        /// </summary>
        [GlobalCleanup]
        public void Cleanup()
        {
            _jsonWriter.Dispose();
            _xmlWriter.Dispose();
        }

        /// <summary>
        /// Builds the values and the documents that are read.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _values = [.. Enumerable.Range(0, Count).Select(i => new Vertex { Flags = i & 7, Id = i, Label = "v" + i, X = i * 0.5, Y = i * 1.5, Z = i * 2.5 })];
            _options = new XmlSerializerOptions { IncludeFields = true, OmitXmlDeclaration = true };
            _jsonOptions = new JsonSerializerOptions { IncludeFields = true };
            var context = new VertexMetadataXmlContext(new XmlSerializerOptions { IncludeFields = true, OmitXmlDeclaration = true });
            _generated = (XmlTypeInfo<List<Vertex>>)context.Options.GetTypeInfo(typeof(List<Vertex>));
            _buffer = new ArrayBufferWriter<byte>(262_144);
            _jsonBuffer = new ArrayBufferWriter<byte>(262_144);
            _xmlWriter = new Utf8XmlWriter(_buffer);
            _jsonWriter = new Utf8JsonWriter(_jsonBuffer);
            _json = JsonSerializer.SerializeToUtf8Bytes(_values, _jsonOptions);
            _xml = EssentialsXmlSerializer.SerializeToUtf8Bytes(_values, _options);
        }

        /// <summary>
        /// System.Text.Json read of 1,000 value-type objects.
        /// </summary>
        /// <returns>The values.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadVertices")]
        public List<Vertex>? Stj_ReadVertices()
        {
            return JsonSerializer.Deserialize<List<Vertex>>(_json, _jsonOptions);
        }

        /// <summary>
        /// System.Text.Json write of 1,000 value-type objects.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteVertices")]
        public long Stj_WriteVertices()
        {
            _jsonBuffer.ResetWrittenCount();
            _jsonWriter.Reset(_jsonBuffer);
            JsonSerializer.Serialize(_jsonWriter, _values, _jsonOptions);
            return _jsonBuffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials read of 1,000 value-type objects through the reflection resolver.
        /// </summary>
        /// <returns>The values.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadVertices")]
        public List<Vertex>? XmlEssentials_ReadVertices()
        {
            return EssentialsXmlSerializer.Deserialize<List<Vertex>>(_xml, _options);
        }

        /// <summary>
        /// XmlEssentials read of 1,000 value-type objects through generated metadata.
        /// </summary>
        /// <returns>The values.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadVertices")]
        public List<Vertex>? XmlEssentials_ReadVertices_SourceGen()
        {
            return EssentialsXmlSerializer.Deserialize(_xml, _generated);
        }

        /// <summary>
        /// XmlEssentials write of 1,000 value-type objects through the reflection resolver.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteVertices")]
        public long XmlEssentials_WriteVertices()
        {
            _buffer.ResetWrittenCount();
            _xmlWriter.Reset(_buffer);
            EssentialsXmlSerializer.Serialize(_xmlWriter, _values, _options);
            _xmlWriter.Flush();
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials write of 1,000 value-type objects through generated metadata.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteVertices")]
        public long XmlEssentials_WriteVertices_SourceGen()
        {
            _buffer.ResetWrittenCount();
            _xmlWriter.Reset(_buffer);
            EssentialsXmlSerializer.Serialize(_xmlWriter, _values, _generated);
            _xmlWriter.Flush();
            return _buffer.WrittenCount;
        }

        #endregion

    }

}
