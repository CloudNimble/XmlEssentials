using System.Runtime.CompilerServices;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Heap-allocation checks for the <see cref="Utf8XmlReader"/> hot path.
    /// </summary>
    [TestClass]
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
