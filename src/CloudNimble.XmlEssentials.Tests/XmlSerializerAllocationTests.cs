using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Heap-allocation checks for <see cref="XmlSerializer"/>, and the contract of the pooled buffer it writes to.
    /// </summary>
    [TestClass]
    public class XmlSerializerAllocationTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Options = new() { OmitXmlDeclaration = true };
        private static readonly byte[] Xml = "<Int32>12345</Int32>"u8.ToArray();

        #endregion

        #region Public Methods

        [TestMethod]
        public void Serialize_ToBufferWriter_DoesNotAllocate()
        {
            var buffer = new ArrayBufferWriter<byte>(256);
            WriteToBuffer(buffer, 12345);
            WriteToBuffer(buffer, 12345);
            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteToBuffer(buffer, 12345);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            buffer.WrittenSpan.SequenceEqual(Xml).Should().BeTrue();
        }

        [TestMethod]
        public void Serialize_Enum_ToBufferWriter_DoesNotAllocate()
        {
            var buffer = new ArrayBufferWriter<byte>(256);
            WriteToBuffer(buffer, AttributeTargets.Class | AttributeTargets.Method);
            WriteToBuffer(buffer, AttributeTargets.Class | AttributeTargets.Method);
            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteToBuffer(buffer, AttributeTargets.Class | AttributeTargets.Method);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            buffer.WrittenSpan.SequenceEqual("<AttributeTargets>Class Method</AttributeTargets>"u8).Should().BeTrue();
        }

        [TestMethod]
        public void Serialize_ToUtf8Bytes_AllocatesOnlyTheResult()
        {
            XmlSerializer.SerializeToUtf8Bytes(12345, Options);
            XmlSerializer.SerializeToUtf8Bytes(12345, Options);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var result = XmlSerializer.SerializeToUtf8Bytes(12345, Options);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            var same = new byte[result.Length];
            var expected = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(expected);
            result.Should().Equal(Xml);
            same.Length.Should().Be(Xml.Length);
        }

        [TestMethod]
        public void Deserialize_FromUtf8_DoesNotAllocate()
        {
            Read<int>(Xml).Should().Be(12345);
            Read<int>(Xml).Should().Be(12345);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var value = Read<int>(Xml);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            value.Should().Be(12345);
        }

        [TestMethod]
        public void Deserialize_DateTime_FromUtf8_DoesNotAllocate()
        {
            var xml = "<DateTime>2020-01-02T03:04:05Z</DateTime>"u8.ToArray();
            var expected = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            Read<DateTime>(xml).Should().Be(expected);
            Read<DateTime>(xml).Should().Be(expected);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var value = Read<DateTime>(xml);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            value.Should().Be(expected);
        }

        [TestMethod]
        public void Deserialize_FromStream_DoesNotAllocate()
        {
            using var stream = new MemoryStream(Xml);
            ReadStream(stream).Should().Be(12345);
            ReadStream(stream).Should().Be(12345);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var value = ReadStream(stream);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            value.Should().Be(12345);
        }

        [TestMethod]
        public void Deserialize_Enum_FromUtf8_DoesNotAllocate()
        {
            var xml = "<AttributeTargets>Class Method</AttributeTargets>"u8.ToArray();
            Read<AttributeTargets>(xml).Should().Be(AttributeTargets.Class | AttributeTargets.Method);
            Read<AttributeTargets>(xml).Should().Be(AttributeTargets.Class | AttributeTargets.Method);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var value = Read<AttributeTargets>(xml);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            value.Should().Be(AttributeTargets.Class | AttributeTargets.Method);
        }

        [TestMethod]
        public void PooledBufferWriter_KeepsWhatWasWritten_AsItGrows()
        {
            var buffer = new XmlPooledBufferWriter();
            buffer.Rent(16);
            IBufferWriter<byte> writer = buffer;
            var expected = new byte[9_000];
            for (var i = 0; i < expected.Length; i++)
            {
                expected[i] = (byte)(i % 251);
            }

            expected.AsSpan(0, 10).CopyTo(writer.GetSpan(10));
            writer.Advance(10);
            expected.AsSpan(10, 4_000).CopyTo(writer.GetMemory(4_000).Span);
            writer.Advance(4_000);
            expected.AsSpan(4_010).CopyTo(writer.GetSpan(4_990));
            writer.Advance(4_990);
            writer.GetSpan().Length.Should().BeGreaterThan(0);

            buffer.WrittenSpan.SequenceEqual(expected).Should().BeTrue();
            buffer.WrittenMemory.Span.SequenceEqual(expected).Should().BeTrue();
            buffer.Return();
            buffer.WrittenSpan.Length.Should().Be(0);
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static T? Read<T>(byte[] xml)
        {
            return XmlSerializer.Deserialize<T>(xml, Options);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReadStream(MemoryStream stream)
        {
            stream.Position = 0;
            return XmlSerializer.Deserialize<int>(stream, Options);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteToBuffer<T>(ArrayBufferWriter<byte> buffer, T value)
        {
            buffer.ResetWrittenCount();
            XmlSerializer.Serialize(buffer, value, Options);
        }

        #endregion

    }

}
