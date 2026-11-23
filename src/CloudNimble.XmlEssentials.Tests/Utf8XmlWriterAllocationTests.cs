using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Heap-allocation checks for the <see cref="Utf8XmlWriter"/> hot path.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class Utf8XmlWriterAllocationTests
    {

        #region Public Methods

        [TestMethod]
        public void WriteNumberValue_DoesNotAllocate()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            WriteInt(writer, 1);
            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteInt(writer, 2);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            writer.WriteEndElement();
        }

        [TestMethod]
        public void WriteBooleanValue_DoesNotAllocate()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            WriteBool(writer, true);
            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteBool(writer, false);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            writer.WriteEndElement();
        }

        [TestMethod]
        public void WriteEmptyElement_Utf8_DoesNotAllocate()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("root");
            WriteEmpty(writer);
            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteEmpty(writer);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            writer.WriteEndElement();
        }

        [TestMethod]
        public void Reset_WriteCatalog_DoesNotAllocate()
        {
            var buffer = new ArrayBufferWriter<byte>(32_768);
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            WriteCatalog(writer);
            writer.Reset();
            buffer.Clear();
            WriteCatalog(writer);
            writer.Reset();
            buffer.Clear();
            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteCatalog(writer);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
        }

        [TestMethod]
        public void WriteCatalogNumbers_AllocatesOnlyTheWriter()
        {
            var buffer = new ArrayBufferWriter<byte>(32_768);
            var options = new XmlWriterOptions { OmitXmlDeclaration = true };
            WriteCatalog(buffer, options);
            buffer.Clear();
            var ctorBefore = GC.GetAllocatedBytesForCurrentThread();
            Ctor(buffer, options);
            var ctorBytes = GC.GetAllocatedBytesForCurrentThread() - ctorBefore;
            WriteCatalog(buffer, options);
            buffer.Clear();
            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteCatalog(buffer, options);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(ctorBytes);
        }

        [TestMethod]
        public void WriteUtf8Text_DoesNotAllocate()
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            WriteUtf8(writer, "hello"u8);
            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteUtf8(writer, "world"u8);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            writer.WriteEndElement();
        }

        #endregion

        #region Private Methods

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteInt(Utf8XmlWriter writer, int value)
        {
            writer.WriteNumberValue(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteBool(Utf8XmlWriter writer, bool value)
        {
            writer.WriteBooleanValue(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteEmpty(Utf8XmlWriter writer)
        {
            writer.WriteEmptyElement("e"u8);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteUtf8(Utf8XmlWriter writer, ReadOnlySpan<byte> value)
        {
            writer.WriteStringValue(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Ctor(ArrayBufferWriter<byte> buffer, XmlWriterOptions options)
        {
            using var writer = new Utf8XmlWriter(buffer, options);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteCatalog(ArrayBufferWriter<byte> buffer, XmlWriterOptions options)
        {
            using var writer = new Utf8XmlWriter(buffer, options);
            WriteCatalog(writer);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteCatalog(Utf8XmlWriter writer)
        {
            writer.WriteStartElement("catalog");
            for (var i = 0; i < 200; i++)
            {
                writer.WriteStartElement("item");
                writer.WriteAttributeNumber("id", i);
                writer.WriteStartElement("name");
                writer.WriteStringValue("Widget");
                writer.WriteNumberValue(i);
                writer.WriteEndElement();
                writer.WriteElementNumber("qty", i);
                writer.WriteElementNumber("price", 9.99d);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }

        #endregion

    }

}
