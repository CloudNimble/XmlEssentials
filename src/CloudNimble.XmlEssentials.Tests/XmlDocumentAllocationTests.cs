using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Heap-allocation checks for <see cref="XmlDocument"/> and <see cref="XmlElement"/>.
    /// </summary>
    [TestClass]
    public class XmlDocumentAllocationTests
    {

        #region Fields

        private static readonly byte[] Catalog = BuildCatalog();

        #endregion

        #region Public Methods

        [TestMethod]
        public void Navigate_DoesNotAllocate()
        {
            using var document = XmlDocument.Parse(Catalog);
            Navigate(document).Should().Be(Navigate(document));
            var before = GC.GetAllocatedBytesForCurrentThread();
            var sum = Navigate(document);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            sum.Should().Be(2 * (199 * 200 / 2));
        }

        [TestMethod]
        public void NamespacedLookup_DoesNotAllocate()
        {
            using var document = XmlDocument.Parse("<p:r xmlns:p=\"urn:catalog\" p:a=\"4\"><p:é>5</p:é></p:r>");
            NamespacedLookup(document).Should().Be(9);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var sum = NamespacedLookup(document);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            sum.Should().Be(9);
        }

        [TestMethod]
        public void Parse_AllocatesOnlyTheDocument()
        {
            ParseAndDispose(Catalog);
            ParseAndDispose(Catalog);
            var before = GC.GetAllocatedBytesForCurrentThread();
            ParseAndDispose(Catalog);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().BeGreaterThan(0).And.BeLessThanOrEqualTo(128);
        }

        [TestMethod]
        public void TypedGetters_DoNotAllocate()
        {
            using var document = XmlDocument.Parse(
                "<r><i>12</i><l>34</l><d>1.5</d><m>2.5</m><b>true</b><t>2020-01-02T03:04:05Z</t>"
                + "<g>00112233-4455-6677-8899-aabbccddeeff</g><x>1<y/>2</x></r>");
            TypedGetters(document).Should().Be(TypedGetters(document));
            var before = GC.GetAllocatedBytesForCurrentThread();
            var sum = TypedGetters(document);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            sum.Should().Be(12 + 34 + 1.5 + 2.5 + 1 + 2020 + 2020 + 12);
        }

        [TestMethod]
        public void WriteTo_DoesNotAllocate()
        {
            using var document = XmlDocument.Parse(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><?pi d?><p:r xmlns:p=\"urn:catalog\" p:a=\"4\"><p:n>5</p:n><m k=\"v\">a&amp;b</m></p:r>");
            var buffer = new ArrayBufferWriter<byte>(4096);
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            Write(document, writer, buffer);
            Write(document, writer, buffer);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Write(document, writer, buffer);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be(
                "<?xml version=\"1.0\" encoding=\"utf-8\"?><?pi d?><p:r xmlns:p=\"urn:catalog\" p:a=\"4\"><p:n>5</p:n><m k=\"v\">a&amp;b</m></p:r>");
        }

        [TestMethod]
        public void WriteTo_Catalog_DoesNotAllocate()
        {
            using var document = XmlDocument.Parse(Catalog);
            var buffer = new ArrayBufferWriter<byte>(32_768);
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            Write(document, writer, buffer);
            Write(document, writer, buffer);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Write(document, writer, buffer);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
            buffer.WrittenSpan.SequenceEqual(Catalog).Should().BeTrue();
        }

        [TestMethod]
        [DataRow("document.xml")]
        [DataRow("libreoffice_document.fodt")]
        [DataRow("rpm_primary.xml")]
        [DataRow("sample_rss.xml")]
        public void WriteTo_RealDocument_DoesNotAllocate(string fixture)
        {
            var xml = System.IO.File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "quick-xml", fixture));
            using var document = XmlDocument.Parse(xml, new XmlDocumentOptions
            {
                CommentHandling = XmlCommentHandling.Allow,
                WhitespaceHandling = XmlWhitespaceHandling.Significant,
            });
            var buffer = new ArrayBufferWriter<byte>(xml.Length * 2);
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            Write(document, writer, buffer);
            Write(document, writer, buffer);
            var before = GC.GetAllocatedBytesForCurrentThread();
            Write(document, writer, buffer);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            allocated.Should().Be(0);
        }

        #endregion

        #region Private Methods

        private static byte[] BuildCatalog()
        {
            var builder = new StringBuilder(16_384);
            builder.Append("<catalog>");
            for (var i = 0; i < 200; i++)
            {
                builder.Append("<item id=\"").Append(i).Append("\"><name>Widget").Append(i)
                    .Append("</name><qty>").Append(i).Append("</qty><price>9.99</price></item>");
            }

            builder.Append("</catalog>");
            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int NamespacedLookup(XmlDocument document)
        {
            var root = document.RootElement;
            return root.GetAttribute("a", "urn:catalog").GetInt32() + root.GetChild("é", "urn:catalog").GetInt32();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Navigate(XmlDocument document)
        {
            var sum = 0;
            foreach (var item in document.RootElement.EnumerateChildren())
            {
                foreach (var attribute in item.EnumerateAttributes())
                {
                    sum += attribute.GetInt32();
                }

                sum += item.GetChild("qty").GetInt32();
            }

            return sum;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ParseAndDispose(byte[] xml)
        {
            using var document = XmlDocument.Parse(xml);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static double TypedGetters(XmlDocument document)
        {
            var root = document.RootElement;
            return root.GetChild("i").GetInt32()
                + root.GetChild("l").GetInt64()
                + root.GetChild("d").GetDouble()
                + (double)root.GetChild("m").GetDecimal()
                + (root.GetChild("b").GetBoolean() ? 1 : 0)
                + root.GetChild("t").GetDateTime().Year
                + root.GetChild("t").GetDateTimeOffset().Year
                + (root.GetChild("g").GetGuid() == Guid.Empty ? 0 : 0)
                + root.GetChild("x").GetInt32();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Write(XmlDocument document, Utf8XmlWriter writer, ArrayBufferWriter<byte> buffer)
        {
            writer.Reset();
            buffer.Clear();
            document.WriteTo(writer);
            writer.Flush();
        }

        #endregion

    }

}
