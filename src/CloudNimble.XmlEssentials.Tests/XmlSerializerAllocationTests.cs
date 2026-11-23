using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
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

        [TestMethod]
        public void Serialize_Objects_ToBufferWriter_DoNotAllocate()
        {
            WritesWithoutAllocating(new Person { Name = "Ada", Age = 36 }, "<Person><Name>Ada</Name><Age>36</Age></Person>");
            WritesWithoutAllocating(
                new Employee { Name = "Ada", Home = new Address { Street = "1 Main", City = "Paris" } },
                "<Employee><Name>Ada</Name><Home><Street>1 Main</Street><City>Paris</City></Home></Employee>");
            WritesWithoutAllocating(new AttributeMapped { Id = 7 }, "<AttributeMapped Id=\"7\"/>");
            WritesWithoutAllocating(new Price { Currency = "USD", Amount = 19.99m }, "<Price Currency=\"USD\">19.99</Price>");
            WritesWithoutAllocating(
                new WrappedCatalog { Title = "t", Items = ["a", "b"] },
                "<WrappedCatalog><Items><Item>a</Item><Item>b</Item></Items><Title>t</Title></WrappedCatalog>");
            WritesWithoutAllocating(new ArrayHolder { Values = [1, 2] }, "<ArrayHolder><Values>1</Values><Values>2</Values></ArrayHolder>");
            WritesWithoutAllocating(
                new ProductCatalog { Items = [new Book { Title = "b", Author = "a" }, new Product { Title = "p" }] },
                "<ProductCatalog><Book><Title>b</Title><Author>a</Author></Book><Items><Title>p</Title></Items></ProductCatalog>");
            WritesWithoutAllocating(new Scorecard { Scores = new() { ["alice"] = 10 } }, "<Scorecard><Scores><alice>10</alice></Scores></Scorecard>");
            WritesWithoutAllocating(new Dictionary<int, int> { [1] = 2 }, "<DictionaryOfInt32Int32><Key>1</Key><Value>2</Value></DictionaryOfInt32Int32>");
            WritesWithoutAllocating(new List<Person> { new() { Name = "Ada", Age = 36 } }, "<ListOfPerson><Person><Name>Ada</Name><Age>36</Age></Person></ListOfPerson>");
            WritesWithoutAllocating(new PersonRecord("Ada", 36), "<PersonRecord><Name>Ada</Name><Age>36</Age></PersonRecord>");
        }

        [TestMethod]
        public void Serialize_NullableValues_ToBufferWriter_DoNotAllocate()
        {
            var list = new List<int?>(1000);
            for (var i = 0; i < 1000; i++)
            {
                list.Add(i);
            }

            var buffer = new ArrayBufferWriter<byte>(65536);
            for (var i = 0; i < 20000; i++)
            {
                WriteToBuffer(buffer, list);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteToBuffer(buffer, list);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            XmlSerializer.Deserialize<List<int?>>(buffer.WrittenSpan, Options).Should().Equal(list);
        }

        [TestMethod]
        public void Serialize_TimeSpan_ToBufferWriter_DoesNotAllocate()
        {
            WritesWithoutAllocating(new TimeSpan(1, 2, 3, 4, 5), "<TimeSpan>P1DT2H3M4.005S</TimeSpan>");
        }

        [TestMethod]
        public void Deserialize_Person_AllocatesOnlyTheObjectAndItsString()
        {
            var xml = "<Person><Name>Ada Bo</Name><Age>36</Age></Person>"u8.ToArray();
            Read<Person>(xml);
            Read<Person>(xml);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var read = Read<Person>(xml)!;
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            var same = new Person { Name = new string('x', 6), Age = 36 };
            var expected = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(expected);
            read.Name.Should().Be("Ada Bo");
            read.Age.Should().Be(36);
            same.Name.Should().HaveLength(read.Name!.Length);
        }

        [TestMethod]
        public void Deserialize_AttributeMapped_AllocatesOnlyTheObject()
        {
            var xml = "<AttributeMapped Id=\"7\"/>"u8.ToArray();
            Read<AttributeMapped>(xml);
            Read<AttributeMapped>(xml);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var read = Read<AttributeMapped>(xml)!;
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            var same = new AttributeMapped { Id = 7 };
            var expected = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(expected);
            read.Id.Should().Be(same.Id);
        }

        [TestMethod]
        public void Deserialize_Price_AllocatesOnlyTheObjectAndItsString()
        {
            var xml = "<Price Currency=\"USD\">19.99</Price>"u8.ToArray();
            Read<Price>(xml);
            Read<Price>(xml);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var read = Read<Price>(xml)!;
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            var same = new Price { Currency = new string('x', 3), Amount = 19.99m };
            var expected = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(expected);
            read.Currency.Should().Be("USD");
            read.Amount.Should().Be(same.Amount);
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

        /// <summary>
        /// Writes <paramref name="value"/> three times and checks that the third write allocates nothing and
        /// produces <paramref name="expected"/>.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="value">The value.</param>
        /// <param name="expected">The whole document.</param>
        private static void WritesWithoutAllocating<T>(T value, string expected)
        {
            var buffer = new ArrayBufferWriter<byte>(1024);
            WriteToBuffer(buffer, value);
            WriteToBuffer(buffer, value);
            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteToBuffer(buffer, value);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be(expected);
            allocated.Should().Be(0, "writing {0} must not allocate", typeof(T));
        }

        #endregion

    }

}
