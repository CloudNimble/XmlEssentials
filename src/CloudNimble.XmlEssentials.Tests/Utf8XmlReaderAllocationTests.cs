using System;
using System.Runtime.CompilerServices;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Heap-allocation checks for the <see cref="Utf8XmlReader"/> hot path.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class Utf8XmlReaderAllocationTests
    {

        #region Public Methods

        [TestMethod]
        public void Drain_EmptyElement_AllocatesNothing()
        {
            var xml = "<e/>"u8.ToArray();
            Drain(xml);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Drain(xml);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
        }

        [TestMethod]
        public void Drain_NestedElements_DoesNotAllocatePerToken()
        {
            var xml = "<a><b><c>x</c></b></a>"u8.ToArray();
            Drain(xml);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Drain(xml);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
        }

        [TestMethod]
        public void Drain_WithAttribute_AllocatesNothing()
        {
            var xml = "<e a=\"1\"/>"u8.ToArray();
            Drain(xml);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Drain(xml);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
        }

        [TestMethod]
        public void Drain_WithDefaultXmlns_AllocatesNothing()
        {
            var xml = "<a xmlns=\"urn:a\"><b/><c>x</c></a>"u8.ToArray();
            Drain(xml);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Drain(xml);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
        }

        [TestMethod]
        public void Drain_WithPrefixedXmlns_AllocatesNothing()
        {
            var xml = "<p:a xmlns:p=\"urn:catalog\"><p:b>x</p:b></p:a>"u8.ToArray();
            Drain(xml);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Drain(xml);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
        }

        [TestMethod]
        public void Drain_DecodedValuesThatKeepGrowing_AllocateBuffersThatDouble()
        {
            var text = new StringBuilder("<r>");
            for (var i = 0; i < 64; i++)
            {
                text.Append("<e>&amp;").Append('x', i).Append("</e>");
            }

            var xml = Encoding.UTF8.GetBytes(text.Append("</r>").ToString());
            Drain(xml);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var tokens = Drain(xml);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

            // The values are 5 to 68 bytes long before decoding, so the buffer is 5, 10, 20, 40 and then 80 bytes.
            before = GC.GetAllocatedBytesForCurrentThread();
            var buffers = new[] { new byte[5], new byte[10], new byte[20], new byte[40], new byte[80] };
            var expected = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            var holder = new byte[5][];
            expected -= GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(expected);
            tokens.Should().Be(2 + (64 * 3));
            buffers.Length.Should().Be(holder.Length);
        }

        [TestMethod]
        public void GetInt32_DoesNotAllocate()
        {
            var xml = "<n>42</n>"u8.ToArray();
            _ = ReadInt(xml);
            var before = GC.GetAllocatedBytesForCurrentThread();
            _ = ReadInt(xml);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Drain(byte[] xml)
        {
            var reader = new Utf8XmlReader(xml);
            var count = 0;
            while (reader.Read())
            {
                count++;
            }

            return count;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReadInt(byte[] xml)
        {
            var reader = new Utf8XmlReader(xml);
            reader.Read();
            reader.Read();
            return reader.GetInt32();
        }

        #endregion

    }

}
