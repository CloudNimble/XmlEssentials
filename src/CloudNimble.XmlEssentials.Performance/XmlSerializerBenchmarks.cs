using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;
using BclXmlSerializer = System.Xml.Serialization.XmlSerializer;
using EssentialsXmlSerializer = CloudNimble.Text.Xml.XmlSerializer;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Compares <see cref="EssentialsXmlSerializer"/> to a cached BCL <see cref="BclXmlSerializer"/> on scalar roots,
    /// with <see cref="JsonSerializer"/> as a sidecar.
    /// </summary>
    /// <remarks>
    /// Each library reads the document it wrote, so root names differ (<c>&lt;Int32&gt;</c>, <c>&lt;int&gt;</c>, a bare
    /// JSON number). BCL serializers and reader/writer settings are created once. BCL has no call that returns bytes,
    /// so its write benches reuse one <see cref="MemoryStream"/> and copy it out.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlSerializerBenchmarks
    {

        #region Fields

        private const string Text = "The quick brown fox & the <lazy> dog";
        private static readonly DateTime Moment = new(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        private BclXmlSerializer _bclDateTime = null!;
        private byte[] _bclDateTimeXml = null!;
        private BclXmlSerializer _bclEnum = null!;
        private byte[] _bclEnumXml = null!;
        private BclXmlSerializer _bclInt32 = null!;
        private byte[] _bclInt32Xml = null!;
        private BclXmlSerializer _bclNullable = null!;
        private MemoryStream _bclOutput = null!;
        private XmlReaderSettings _bclReaderSettings = null!;
        private BclXmlSerializer _bclString = null!;
        private byte[] _bclStringXml = null!;
        private XmlWriterSettings _bclWriterSettings = null!;
        private ArrayBufferWriter<byte> _buffer = null!;
        private byte[] _dateTimeXml = null!;
        private byte[] _enumXml = null!;
        private byte[] _int32Json = null!;
        private byte[] _int32Xml = null!;
        private MemoryStream _int32XmlStream = null!;
        private MemoryStream _output = null!;
        private byte[] _stringJson = null!;
        private byte[] _stringXml = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// BCL read of a <see cref="DateTime"/> root.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadDateTime")]
        public DateTime Bcl_ReadDateTime()
        {
            return (DateTime)ReadBcl(_bclDateTime, _bclDateTimeXml)!;
        }

        /// <summary>
        /// BCL read of an enum root.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadEnum")]
        public DayOfWeek Bcl_ReadEnum()
        {
            return (DayOfWeek)ReadBcl(_bclEnum, _bclEnumXml)!;
        }

        /// <summary>
        /// BCL read of an <see cref="int"/> root.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadInt32")]
        public int Bcl_ReadInt32()
        {
            return (int)ReadBcl(_bclInt32, _bclInt32Xml)!;
        }

        /// <summary>
        /// BCL read of an <see cref="int"/> root from a stream.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadInt32Stream")]
        public int Bcl_ReadInt32Stream()
        {
            return (int)ReadBcl(_bclInt32, _bclInt32Xml)!;
        }

        /// <summary>
        /// BCL read of a <see cref="string"/> root.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ReadString")]
        public string Bcl_ReadString()
        {
            return (string)ReadBcl(_bclString, _bclStringXml)!;
        }

        /// <summary>
        /// BCL write of a <see cref="DateTime"/> root.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteDateTime")]
        public byte[] Bcl_WriteDateTime()
        {
            return WriteBcl(_bclDateTime, Moment);
        }

        /// <summary>
        /// BCL write of an enum root.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteEnum")]
        public byte[] Bcl_WriteEnum()
        {
            return WriteBcl(_bclEnum, DayOfWeek.Wednesday);
        }

        /// <summary>
        /// BCL write of an <see cref="int"/> root.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteInt32")]
        public byte[] Bcl_WriteInt32()
        {
            return WriteBcl(_bclInt32, 12345);
        }

        /// <summary>
        /// BCL write of an <see cref="int"/> root to a reused stream, without copying it out.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteInt32Buffer")]
        public long Bcl_WriteInt32Buffer()
        {
            WriteBclToOutput(_bclInt32, 12345);
            return _bclOutput.Length;
        }

        /// <summary>
        /// BCL write of an <see cref="int"/> root as a string.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteInt32String")]
        public string Bcl_WriteInt32String()
        {
            WriteBclToOutput(_bclInt32, 12345);
            return Encoding.UTF8.GetString(_bclOutput.GetBuffer(), 0, (int)_bclOutput.Length);
        }

        /// <summary>
        /// BCL write of a null <see cref="Nullable{T}"/> root.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteNullable")]
        public byte[] Bcl_WriteNullable()
        {
            return WriteBcl(_bclNullable, null);
        }

        /// <summary>
        /// BCL write of a <see cref="string"/> root.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("WriteString")]
        public byte[] Bcl_WriteString()
        {
            return WriteBcl(_bclString, Text);
        }

        /// <summary>
        /// Releases the reused streams.
        /// </summary>
        [GlobalCleanup]
        public void Cleanup()
        {
            _bclOutput.Dispose();
            _int32XmlStream.Dispose();
            _output.Dispose();
        }

        /// <summary>
        /// Builds serializers, settings, and payloads once.
        /// </summary>
        [GlobalSetup]
        public void Setup()
        {
            _bclDateTime = new BclXmlSerializer(typeof(DateTime));
            _bclEnum = new BclXmlSerializer(typeof(DayOfWeek));
            _bclInt32 = new BclXmlSerializer(typeof(int));
            _bclNullable = new BclXmlSerializer(typeof(int?));
            _bclString = new BclXmlSerializer(typeof(string));
            _bclOutput = new MemoryStream(1024);
            _bclReaderSettings = new XmlReaderSettings { CloseInput = false };
            _bclWriterSettings = new XmlWriterSettings { CloseOutput = false, Encoding = new UTF8Encoding(false) };
            _bclDateTimeXml = WriteBcl(_bclDateTime, Moment);
            _bclEnumXml = WriteBcl(_bclEnum, DayOfWeek.Wednesday);
            _bclInt32Xml = WriteBcl(_bclInt32, 12345);
            _bclStringXml = WriteBcl(_bclString, Text);
            _buffer = new ArrayBufferWriter<byte>(1024);
            _dateTimeXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(Moment);
            _enumXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(DayOfWeek.Wednesday);
            _int32Json = JsonSerializer.SerializeToUtf8Bytes(12345);
            _int32Xml = EssentialsXmlSerializer.SerializeToUtf8Bytes(12345);
            _int32XmlStream = new MemoryStream(_int32Xml);
            _output = new MemoryStream(1024);
            _stringJson = JsonSerializer.SerializeToUtf8Bytes(Text);
            _stringXml = EssentialsXmlSerializer.SerializeToUtf8Bytes(Text);
        }

        /// <summary>
        /// System.Text.Json read of an <see cref="int"/>.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadInt32")]
        public int Stj_ReadInt32()
        {
            return JsonSerializer.Deserialize<int>(_int32Json);
        }

        /// <summary>
        /// System.Text.Json read of a <see cref="string"/>.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadString")]
        public string? Stj_ReadString()
        {
            return JsonSerializer.Deserialize<string>(_stringJson);
        }

        /// <summary>
        /// System.Text.Json write of an <see cref="int"/>.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteInt32")]
        public byte[] Stj_WriteInt32()
        {
            return JsonSerializer.SerializeToUtf8Bytes(12345);
        }

        /// <summary>
        /// System.Text.Json write of an <see cref="int"/> as a string.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteInt32String")]
        public string Stj_WriteInt32String()
        {
            return JsonSerializer.Serialize(12345);
        }

        /// <summary>
        /// System.Text.Json write of a <see cref="string"/>.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteString")]
        public byte[] Stj_WriteString()
        {
            return JsonSerializer.SerializeToUtf8Bytes(Text);
        }

        /// <summary>
        /// XmlEssentials read of a <see cref="DateTime"/> root.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadDateTime")]
        public DateTime XmlEssentials_ReadDateTime()
        {
            return EssentialsXmlSerializer.Deserialize<DateTime>(_dateTimeXml);
        }

        /// <summary>
        /// XmlEssentials read of an enum root.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadEnum")]
        public DayOfWeek XmlEssentials_ReadEnum()
        {
            return EssentialsXmlSerializer.Deserialize<DayOfWeek>(_enumXml);
        }

        /// <summary>
        /// XmlEssentials read of an <see cref="int"/> root.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadInt32")]
        public int XmlEssentials_ReadInt32()
        {
            return EssentialsXmlSerializer.Deserialize<int>(_int32Xml);
        }

        /// <summary>
        /// XmlEssentials read of an <see cref="int"/> root from a stream.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadInt32Stream")]
        public int XmlEssentials_ReadInt32Stream()
        {
            _int32XmlStream.Position = 0;
            return EssentialsXmlSerializer.Deserialize<int>(_int32XmlStream);
        }

        /// <summary>
        /// XmlEssentials read of a <see cref="string"/> root.
        /// </summary>
        /// <returns>The value.</returns>
        [Benchmark]
        [BenchmarkCategory("ReadString")]
        public string? XmlEssentials_ReadString()
        {
            return EssentialsXmlSerializer.Deserialize<string>(_stringXml);
        }

        /// <summary>
        /// XmlEssentials write of a <see cref="DateTime"/> root.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteDateTime")]
        public byte[] XmlEssentials_WriteDateTime()
        {
            return EssentialsXmlSerializer.SerializeToUtf8Bytes(Moment);
        }

        /// <summary>
        /// XmlEssentials write of an enum root.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteEnum")]
        public byte[] XmlEssentials_WriteEnum()
        {
            return EssentialsXmlSerializer.SerializeToUtf8Bytes(DayOfWeek.Wednesday);
        }

        /// <summary>
        /// XmlEssentials write of an <see cref="int"/> root.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteInt32")]
        public byte[] XmlEssentials_WriteInt32()
        {
            return EssentialsXmlSerializer.SerializeToUtf8Bytes(12345);
        }

        /// <summary>
        /// XmlEssentials write of an <see cref="int"/> root to a reused buffer writer.
        /// </summary>
        /// <returns>The document length.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteInt32Buffer")]
        public long XmlEssentials_WriteInt32Buffer()
        {
            _buffer.ResetWrittenCount();
            EssentialsXmlSerializer.Serialize(_buffer, 12345);
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials write of an <see cref="int"/> root as a string.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteInt32String")]
        public string XmlEssentials_WriteInt32String()
        {
            return EssentialsXmlSerializer.Serialize(12345);
        }

        /// <summary>
        /// XmlEssentials write of a null <see cref="Nullable{T}"/> root.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteNullable")]
        public byte[] XmlEssentials_WriteNullable()
        {
            return EssentialsXmlSerializer.SerializeToUtf8Bytes<int?>(null);
        }

        /// <summary>
        /// XmlEssentials write of a <see cref="string"/> root.
        /// </summary>
        /// <returns>The document.</returns>
        [Benchmark]
        [BenchmarkCategory("WriteString")]
        public byte[] XmlEssentials_WriteString()
        {
            return EssentialsXmlSerializer.SerializeToUtf8Bytes(Text);
        }

        #endregion

        #region Private Methods

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
        /// Writes <paramref name="value"/> with a cached BCL serializer and copies the document out.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="value">The value.</param>
        /// <returns>The document.</returns>
        private byte[] WriteBcl(BclXmlSerializer serializer, object? value)
        {
            WriteBclToOutput(serializer, value);
            return _bclOutput.ToArray();
        }

        /// <summary>
        /// Writes <paramref name="value"/> with a cached BCL serializer to the reused stream.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        /// <param name="value">The value.</param>
        private void WriteBclToOutput(BclXmlSerializer serializer, object? value)
        {
            _bclOutput.SetLength(0);
            using var writer = XmlWriter.Create(_bclOutput, _bclWriterSettings);
            serializer.Serialize(writer, value);
        }

        #endregion

    }

}
