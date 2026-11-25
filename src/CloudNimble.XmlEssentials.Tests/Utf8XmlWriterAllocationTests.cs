using System;
using System.Buffers;
using System.Linq;
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
        [DataRow("\n", DisplayName = "line breaks written as they are")]
        [DataRow("\r\n", DisplayName = "line breaks written as CR LF, which takes the escaping path")]
        public void Reset_WriteLongEscapedText_DoesNotAllocate(string newLine)
        {
            // Text that is longer than any buffer on the stack, with characters that are escaped, characters
            // outside ASCII, and line breaks: it is transcoded a part at a time and nothing is allocated for it.
            var text = string.Concat(Enumerable.Repeat("Leave at the back door & ring twice: café \U0001F600 <open>\n", 400));
            var buffer = new ArrayBufferWriter<byte>(262_144);
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { NewLine = newLine, OmitXmlDeclaration = true });
            for (var i = 0; i < 3; i++)
            {
                writer.Reset();
                buffer.Clear();
                WriteLongText(writer, text);
            }

            writer.Reset();
            buffer.Clear();
            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteLongText(writer, text);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
        }

        [TestMethod]
        public void Reset_WriteValidatedRawValue_DoesNotAllocate()
        {
            // The raw value is read to check it, inside a copy of the element that is open. The copy is built in
            // an array from the pool, so a writer that has checked one raw value allocates nothing for the next.
            var raw = System.Text.Encoding.UTF8.GetBytes(string.Concat(Enumerable.Range(0, 200).Select(i => $"<item id=\"{i}\"><name>Widget</name></item>")));
            var buffer = new ArrayBufferWriter<byte>(65_536);
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            for (var i = 0; i < 3; i++)
            {
                writer.Reset();
                buffer.Clear();
                WriteRaw(writer, raw);
            }

            writer.Reset();
            buffer.Clear();
            var before = GC.GetAllocatedBytesForCurrentThread();
            WriteRaw(writer, raw);
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
        private static void WriteLongText(Utf8XmlWriter writer, string text)
        {
            writer.WriteStartElement("notes");
            writer.WriteAttributeString("first", text);
            writer.WriteElementString("note", text);
            writer.WriteStartElement("note");
            writer.WriteStringValue(text.AsSpan());
            writer.WriteEndElement();
            writer.WriteEndElement();
            writer.Flush();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void WriteRaw(Utf8XmlWriter writer, byte[] raw)
        {
            writer.WriteStartElement("catalog");
            writer.WriteRawValue(raw);
            writer.WriteEndElement();
            writer.Flush();
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
