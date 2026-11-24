using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="XmlSerializer"/> over streams, pipes, and asynchronous sequences: complete reads, size limits,
    /// what reaches the destination when a write fails, and reuse of the writer between calls.
    /// </summary>
    [TestClass]
    public class XmlSerializerStreamingTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };
        private static readonly XmlSerializerOptions StrictNumbers = new() { NumberHandling = XmlNumberHandling.Strict, OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        [DataRow(1)]
        [DataRow(7)]
        [DataRow(100_000)]
        public async Task Stream_ThatReturnsFewBytesPerRead_IsReadToItsEnd(int chunk)
        {
            var text = new string('x', 40_000) + "é";
            var utf8 = Encoding.UTF8.GetBytes("<String>" + text + "</String>");
            XmlSerializer.Deserialize<string>(new TrickleStream(utf8, chunk)).Should().Be(text);
            (await XmlSerializer.DeserializeAsync<string>(new TrickleStream(utf8, chunk))).Should().Be(text);
        }

        [TestMethod]
        public async Task MaxDocumentByteCount_StopsAStreamThatIsTooLong_AndAllowsOneExactlyAtTheLimit()
        {
            var utf8 = "<Int32>42</Int32>"u8.ToArray();
            var exact = new XmlSerializerOptions { MaxDocumentByteCount = utf8.Length };
            XmlSerializer.Deserialize<int>(new MemoryStream(utf8), exact).Should().Be(42);
            (await XmlSerializer.DeserializeAsync<int>(new MemoryStream(utf8), exact)).Should().Be(42);

            var small = new XmlSerializerOptions { MaxDocumentByteCount = 4 };
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>(new MemoryStream(utf8), small))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("MaxDocumentByteCount exceeded (4).");
            (await FluentActions.Awaiting(() => XmlSerializer.DeserializeAsync<int>(new MemoryStream(utf8), small).AsTask())
                .Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be("MaxDocumentByteCount exceeded (4).");

            var large = Encoding.UTF8.GetBytes("<String>" + new string('x', 70_000) + "</String>");
            var atLimit = new XmlSerializerOptions { MaxDocumentByteCount = large.Length };
            XmlSerializer.Deserialize<string>(new TrickleStream(large, 5_000), atLimit)!.Length.Should().Be(70_000);
            (await XmlSerializer.DeserializeAsync<string>(new TrickleStream(large, 5_000), atLimit))!.Length.Should().Be(70_000);
            var oneShort = new XmlSerializerOptions { MaxDocumentByteCount = large.Length - 1 };
            FluentActions.Invoking(() => XmlSerializer.Deserialize<string>(new TrickleStream(large, 5_000), oneShort))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be($"MaxDocumentByteCount exceeded ({large.Length - 1}).");
            (await FluentActions.Awaiting(() => XmlSerializer.DeserializeAsync<string>(new TrickleStream(large, 5_000), oneShort).AsTask())
                .Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be($"MaxDocumentByteCount exceeded ({large.Length - 1}).");

            var pooledSize = new XmlSerializerOptions { MaxDocumentByteCount = 16_384 };
            var fills = Encoding.UTF8.GetBytes("<String>" + new string('x', 16_384 - 17) + "</String>");
            XmlSerializer.Deserialize<string>(new MemoryStream(fills), pooledSize)!.Length.Should().Be(16_384 - 17);
            (await XmlSerializer.DeserializeAsync<string>(new MemoryStream(fills), pooledSize))!.Length.Should().Be(16_384 - 17);
            var overflows = Encoding.UTF8.GetBytes("<String>" + new string('x', 16_384 - 16) + "</String>");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<string>(new MemoryStream(overflows), pooledSize))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("MaxDocumentByteCount exceeded (16384).");
            (await FluentActions.Awaiting(() => XmlSerializer.DeserializeAsync<string>(new MemoryStream(overflows), pooledSize).AsTask())
                .Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be("MaxDocumentByteCount exceeded (16384).");
        }

        [TestMethod]
        [Timeout(60_000)]
        public async Task Pipe_DeliveredInSeveralWrites_IsReadUntilItCompletes()
        {
            var pipe = new Pipe();
            var reading = XmlSerializer.DeserializeAsync<int>(pipe.Reader, Bare).AsTask();
            await pipe.Writer.WriteAsync("<Int"u8.ToArray());
            await pipe.Writer.WriteAsync("32>4"u8.ToArray());
            reading.IsCompleted.Should().BeFalse();
            await pipe.Writer.WriteAsync("2</Int32>"u8.ToArray());
            await pipe.Writer.CompleteAsync();
            (await reading).Should().Be(42);
            (await pipe.Reader.ReadAsync()).Buffer.Length.Should().Be(0);

            var text = new string('x', 100_000);
            var bounded = new Pipe(new PipeOptions(pauseWriterThreshold: 64, resumeWriterThreshold: 32));
            var large = XmlSerializer.DeserializeAsync<string>(bounded.Reader, Bare).AsTask();
            await bounded.Writer.WriteAsync(Encoding.UTF8.GetBytes("<String>" + text + "</String>"));
            await bounded.Writer.CompleteAsync();
            (await large).Should().Be(text);

            var limited = new Pipe();
            var small = new XmlSerializerOptions { MaxDocumentByteCount = 4 };
            var failing = XmlSerializer.DeserializeAsync<int>(limited.Reader, small).AsTask();
            await limited.Writer.WriteAsync("<Int32>42"u8.ToArray());
            (await FluentActions.Awaiting(() => failing).Should().ThrowExactlyAsync<XmlException>())
                .Which.Message.Should().Be("MaxDocumentByteCount exceeded (4).");
        }

        [TestMethod]
        public async Task AsyncEnumerable_WritesChildrenOfOneArrayElement_AndReadsThemBack()
        {
            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(stream, Values(1, 2, 3), Bare);
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<ArrayOfInt32><Int32>1</Int32><Int32>2</Int32><Int32>3</Int32></ArrayOfInt32>");
            stream.Position = 0;
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<int>(stream, Bare))).Should().Equal(1, 2, 3);
            stream.Position = 0;
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable(stream, Bare.GetTypeInfo<int>()))).Should().Equal(1, 2, 3);

            using var empty = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(empty, Values<int>(), Bare.GetTypeInfo<int>());
            Encoding.UTF8.GetString(empty.ToArray()).Should().Be("<ArrayOfInt32/>");
            empty.Position = 0;
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<int>(empty, Bare))).Should().BeEmpty();

            using var nulls = new MemoryStream(Encoding.UTF8.GetBytes(
                "<ArrayOfInt32><!--c--><Int32>1</Int32> <Int32 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></ArrayOfInt32>"));
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<int?>(nulls, Bare))).Should().Equal(1, null);
        }

        [TestMethod]
        public async Task AsyncEnumerable_WithoutOptions_UsesTheDefaults()
        {
            const string declaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            using var wrapped = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(wrapped, Values(1, 2));
            Encoding.UTF8.GetString(wrapped.ToArray()).Should().Be(declaration + "<ArrayOfInt32><Int32>1</Int32><Int32>2</Int32></ArrayOfInt32>");
            wrapped.Position = 0;
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<int>(wrapped))).Should().Equal(1, 2);

            using var topLevel = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(topLevel, Values(1, 2), topLevelValues: true);
            Encoding.UTF8.GetString(topLevel.ToArray()).Should().Be(declaration + "<Int32>1</Int32>" + declaration + "<Int32>2</Int32>");
            topLevel.Position = 0;
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<int>(topLevel, topLevelValues: true))).Should().Equal(1, 2);
        }

        [TestMethod]
        public async Task AsyncEnumerable_ReturnsEachValue_AsSoonAsTheStreamHasDeliveredIt()
        {
            var xml = new StringBuilder("<ArrayOfInt32>");
            for (var i = 0; i < 1_000; i++)
            {
                xml.Append("<Int32>").Append(i).Append("</Int32>");
            }

            xml.Append("</ArrayOfInt32>");
            var stream = new TrickleStream(Encoding.UTF8.GetBytes(xml.ToString()), 64);
            var bytesReadAtEachValue = new List<int>();
            var read = new List<int>();
            await foreach (var value in XmlSerializer.DeserializeAsyncEnumerable<int>(stream, Bare))
            {
                read.Add(value);
                bytesReadAtEachValue.Add(stream.BytesRead);
            }

            read.Should().Equal(Enumerable.Range(0, 1_000));
            bytesReadAtEachValue[0].Should().Be(64);
            bytesReadAtEachValue[1].Should().Be(64);
            bytesReadAtEachValue[2].Should().Be(64);
            bytesReadAtEachValue[3].Should().Be(128);
            bytesReadAtEachValue[999].Should().Be(stream.BytesRead);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task AsyncEnumerable_GivesTheSameValues_HoweverTheStreamIsCutUp(bool topLevelValues)
        {
            const string nil = "<String xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>";
            const string items = "<String>a&amp;b</String>\r\n<!--c--><String/>" + nil + "<String><![CDATA[é☃]]>𝄞</String> <String>" + "0123456789012345678901234567890123456789" + "</String>";
            var xml = topLevelValues
                ? "<?xml version=\"1.0\"?>" + items.Replace("\r\n", "\r\n<?xml version=\"1.0\" encoding=\"utf-8\"?>") + "\n<!--end-->\n"
                : "<?xml version=\"1.0\"?>\n<ArrayOfString>" + items + "</ArrayOfString><!--end-->\n";
            var utf8 = Encoding.UTF8.GetBytes(xml);
            var small = new XmlSerializerOptions { DefaultBufferSize = 16 };
            for (var chunk = 1; chunk <= utf8.Length; chunk++)
            {
                var values = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<string>(new TrickleStream(utf8, chunk), topLevelValues, small));
                values.Should().Equal("a&b", "", null, "é☃𝄞", "0123456789012345678901234567890123456789");
            }
        }

        [TestMethod]
        [DataRow("1.0", "a\u0085b\u2028c", DisplayName = "XML 1.0 keeps NEL and LS")]
        [DataRow("1.1", "a\nb\nc", DisplayName = "XML 1.1 reads NEL and LS as line ends")]
        public async Task AsyncEnumerable_ScalarElements_GiveTheSameValues_HoweverTheStreamIsCutUp(string version, string lineEnds)
        {
            var xml = $"<?xml version=\"{version}\"?><ArrayOfString><String>plain</String><String> padded </String><String>longer than the short scan</String>"
                + "<String>a\u0085b\u2028c</String><String>x\r\ny\rz</String><String>a&amp;b</String><String>&#x9F;</String><String>tab\there</String ><String>caf\u00E9</String></ArrayOfString>";
            var utf8 = Encoding.UTF8.GetBytes(xml);
            var small = new XmlSerializerOptions { DefaultBufferSize = 16 };
            for (var chunk = 1; chunk <= utf8.Length; chunk++)
            {
                var values = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<string>(new TrickleStream(utf8, chunk), small));
                values.Should().Equal("plain", " padded ", "longer than the short scan", lineEnds, "x\ny\nz", "a&b", "\u009F", "tab\there", "caf\u00E9");
            }
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public async Task AsyncEnumerable_WithACustomConverter_GivesTheSameValues_HoweverTheStreamIsCutUp(bool topLevelValues)
        {
            const string nil = "<DoublingNumber xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\">gone</DoublingNumber>";
            const string items = "<DoublingNumber>2</DoublingNumber><!--c-->" + nil + "<DoublingNumber><![CDATA[4]]>0</DoublingNumber>\n<DoublingNumber>6</DoublingNumber>";
            var utf8 = Encoding.UTF8.GetBytes(topLevelValues ? items : "<ArrayOfDoublingNumber>" + items + "</ArrayOfDoublingNumber>");
            var small = new XmlSerializerOptions { DefaultBufferSize = 16 };
            for (var chunk = 1; chunk <= utf8.Length; chunk++)
            {
                var values = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<DoublingNumber?>(new TrickleStream(utf8, chunk), topLevelValues, small));
                values.Select(value => value?.Value).Should().Equal(1, null, 20, 3);
            }
        }

        [TestMethod]
        public async Task AsyncEnumerable_InADefaultNamespace_WritesTheArrayElementInIt()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, DefaultNamespace = "urn:x" };
            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(stream, Values(1, 2), options);
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<ArrayOfInt32 xmlns=\"urn:x\"><Int32>1</Int32><Int32>2</Int32></ArrayOfInt32>");
            stream.Position = 0;
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<int>(stream, options))).Should().Equal(1, 2);

            using var empty = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(empty, Values<int>(), options);
            Encoding.UTF8.GetString(empty.ToArray()).Should().Be("<ArrayOfInt32 xmlns=\"urn:x\"/>");

            using var documents = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(documents, Values(1, 2), topLevelValues: true, options);
            Encoding.UTF8.GetString(documents.ToArray()).Should().Be("<Int32 xmlns=\"urn:x\">1</Int32><Int32 xmlns=\"urn:x\">2</Int32>");
        }

        [TestMethod]
        public async Task AsyncEnumerable_WithPreserve_SharesOneSetOfReferencesAcrossTheDocument()
        {
            const string refs = "xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\"";
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.Preserve, DefaultBufferSize = 16 };
            var shared = new RefLeaf { Name = "a" };
            var other = new RefLeaf { Name = "b" };
            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(
                stream,
                Values(new RefPair { First = shared, Second = shared }, new RefPair { First = shared, Second = other }),
                options);
            var utf8 = stream.ToArray();
            Encoding.UTF8.GetString(utf8).Should().Be(
                "<ArrayOfRefPair>" +
                "<RefPair " + refs + " ns1:id=\"1\"><First ns1:id=\"2\"><Name>a</Name></First><Second ns1:ref=\"2\"/></RefPair>" +
                // The writer numbers the prefixes it makes up through the whole document, so the second value gets the next one.
                "<RefPair xmlns:ns2=\"urn:CloudNimble.Text.Xml:refs\" ns2:id=\"3\"><First ns2:ref=\"2\"/><Second ns2:id=\"4\"><Name>b</Name></Second></RefPair>" +
                "</ArrayOfRefPair>");

            // A value is read only once all of it has arrived, so however the stream is cut up, each id is seen once.
            for (var chunk = 1; chunk <= utf8.Length; chunk++)
            {
                var read = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<RefPair>(new TrickleStream(utf8, chunk), options));
                read.Should().HaveCount(2);
                read[0]!.First!.Name.Should().Be("a");
                read[0]!.Second.Should().BeSameAs(read[0]!.First);
                read[1]!.First.Should().BeSameAs(read[0]!.First, "the second value refers to an object the first value defined");
                read[1]!.Second!.Name.Should().Be("b");
            }
        }

        [TestMethod]
        public async Task AsyncEnumerable_WithPreserve_TopLevelValues_EachDocumentHasItsOwnReferences()
        {
            const string refs = "xmlns:ns1=\"urn:CloudNimble.Text.Xml:refs\"";
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.Preserve, DefaultBufferSize = 16 };
            var shared = new RefLeaf { Name = "a" };
            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(
                stream,
                Values(new RefPair { First = shared, Second = shared }, new RefPair { First = shared }),
                topLevelValues: true,
                options);
            var utf8 = stream.ToArray();
            Encoding.UTF8.GetString(utf8).Should().Be(
                "<RefPair " + refs + " ns1:id=\"1\"><First ns1:id=\"2\"><Name>a</Name></First><Second ns1:ref=\"2\"/></RefPair>" +
                "<RefPair " + refs + " ns1:id=\"1\"><First ns1:id=\"2\"><Name>a</Name></First><Second xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></RefPair>");

            for (var chunk = 1; chunk <= utf8.Length; chunk++)
            {
                var read = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<RefPair>(new TrickleStream(utf8, chunk), topLevelValues: true, options));
                read.Should().HaveCount(2);
                read[0]!.Second.Should().BeSameAs(read[0]!.First);
                read[1]!.First!.Name.Should().Be("a");
                read[1]!.First.Should().NotBeSameAs(read[0]!.First, "each document is read on its own");
                read[1]!.Second.Should().BeNull();
            }
        }

        [TestMethod]
        public async Task AsyncEnumerable_WithIgnoreCycles_WritesACycleAsNull()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true, ReferenceHandler = XmlReferenceHandler.IgnoreCycles };
            var root = new RefLeaf { Name = "r" };
            root.Children.Add(root);
            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(stream, Values(root, root), options);
            const string item = "<RefLeaf><Children xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/><Name>r</Name></RefLeaf>";
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<ArrayOfRefLeaf>" + item + item + "</ArrayOfRefLeaf>", "an object written twice in a row is not a cycle");

            var pipe = new Pipe();
            await XmlSerializer.SerializeAsyncEnumerable(pipe.Writer, Values(root), topLevelValues: false, options);
            await pipe.Writer.CompleteAsync();
            var piped = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<RefLeaf>(pipe.Reader, topLevelValues: false, options));
            piped.Should().HaveCount(1);
            piped[0]!.Name.Should().Be("r");
            piped[0]!.Children.Should().Equal([null]);
        }

        [TestMethod]
        public async Task AsyncEnumerable_InANamespace_KeepsItsScopeAcrossReads()
        {
            const string xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<a:ArrayOfString xmlns:a=\"urn:x\" xmlns=\"urn:x\">\n"
                + "  <String>one</String>\n  <a:String>two</a:String>\n  <b:String xmlns:b=\"urn:x\">three</b:String>\n"
                + "  <String xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>\n  <a:String>five</a:String>\n</a:ArrayOfString>";
            var utf8 = Encoding.UTF8.GetBytes(xml);
            var options = new XmlSerializerOptions { DefaultBufferSize = 16, DefaultNamespace = "urn:x" };
            for (var chunk = 1; chunk <= utf8.Length; chunk++)
            {
                var values = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<string>(new TrickleStream(utf8, chunk), options));
                values.Should().Equal("one", "two", "three", null, "five");
            }
        }

        [TestMethod]
        public async Task AsyncEnumerable_ReturnsTheValuesBeforeAMalformedPart_ThenThrows()
        {
            var stream = new TrickleStream("<ArrayOfInt32><Int32>1</Int32><Int32>2</Int32><Int32>x</Int32><Int32>4</Int32></ArrayOfInt32>"u8.ToArray(), 16);
            var read = new List<int>();
            (await FluentActions.Awaiting(async () =>
            {
                await foreach (var value in XmlSerializer.DeserializeAsyncEnumerable<int>(stream, Bare))
                {
                    read.Add(value);
                }
            }).Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be("The value is not a valid integer. Path: /Int32 | LineNumber: 1 | BytePositionInLine: 17.");
            read.Should().Equal(1, 2);

            var truncated = new TrickleStream("<ArrayOfInt32><Int32>1</Int32><Int32>2</Int"u8.ToArray(), 16);
            read.Clear();
            await FluentActions.Awaiting(async () =>
            {
                await foreach (var value in XmlSerializer.DeserializeAsyncEnumerable<int>(truncated, Bare))
                {
                    read.Add(value);
                }
            }).Should().ThrowExactlyAsync<XmlException>();
            read.Should().Equal(1);

            var limited = new XmlSerializerOptions { MaxDocumentByteCount = 40 };
            var tooLong = new TrickleStream("<ArrayOfInt32><Int32>1</Int32><Int32>2</Int32><Int32>3</Int32></ArrayOfInt32>"u8.ToArray(), 16);
            read.Clear();
            await FluentActions.Awaiting(async () =>
            {
                await foreach (var value in XmlSerializer.DeserializeAsyncEnumerable<int>(tooLong, limited))
                {
                    read.Add(value);
                }
            }).Should().ThrowExactlyAsync<XmlException>();
            read.Should().Equal(1);
        }

        [TestMethod]
        public async Task AsyncEnumerable_ConsumerMayStopEarly()
        {
            using var stream = new MemoryStream("<ArrayOfInt32><Int32>1</Int32><Int32>2</Int32><Int32>3</Int32></ArrayOfInt32>"u8.ToArray());
            var read = new List<int>();
            await foreach (var value in XmlSerializer.DeserializeAsyncEnumerable<int>(stream, Bare))
            {
                read.Add(value);
                if (value == 2)
                {
                    break;
                }
            }

            read.Should().Equal(1, 2);
        }

        [TestMethod]
        public async Task AsyncEnumerable_TopLevelValues_AreSuccessiveDocuments()
        {
            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(stream, Values(1, 2), topLevelValues: true, Bare);
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<Int32>1</Int32><Int32>2</Int32>");
            stream.Position = 0;
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<int>(stream, topLevelValues: true, Bare))).Should().Equal(1, 2);

            using var declared = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(declared, Values("a", ""), XmlSerializerOptions.Default.GetTypeInfo<string>(), topLevelValues: true);
            const string declaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
            Encoding.UTF8.GetString(declared.ToArray()).Should().Be(declaration + "<String>a</String>" + declaration + "<String></String>");

            using var emptyElements = new MemoryStream("<String/><String>a</String><String/>"u8.ToArray());
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable(emptyElements, Bare.GetTypeInfo<string>(), topLevelValues: true)))
                .Should().Equal("", "a", "");

            using var separated = new MemoryStream(Encoding.UTF8.GetBytes(
                declaration + "<String>a</String>\r\n" + declaration + "<!--before--><String>b</String><!--after-->\n"));
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<string>(separated, topLevelValues: true))).Should().Equal("a", "b");

            using var nothing = new MemoryStream();
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<string>(nothing, topLevelValues: true))).Should().BeEmpty();

            using var wrongName = new MemoryStream("<Int32>1</Int32><Int64>2</Int64>"u8.ToArray());
            (await FluentActions.Awaiting(() => ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<int>(wrongName, topLevelValues: true, Bare)))
                .Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be("The root element does not match 'Int32'.");
        }

        [TestMethod]
        public async Task AsyncEnumerable_OverAPipe()
        {
            var wrapped = new Pipe();
            await XmlSerializer.SerializeAsyncEnumerable(wrapped.Writer, Values(1, 2), topLevelValues: false, Bare);
            await wrapped.Writer.CompleteAsync();
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<int>(wrapped.Reader, topLevelValues: false, Bare))).Should().Equal(1, 2);

            var typed = new Pipe();
            await typed.Writer.WriteAsync("<ArrayOfInt32><Int32>3</Int32></ArrayOfInt32>"u8.ToArray());
            await typed.Writer.CompleteAsync();
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable(typed.Reader, Bare.GetTypeInfo<int>()))).Should().Equal(3);
        }

        [TestMethod]
        public async Task AsyncEnumerable_LongerThanTheWriterBuffer_IsWrittenCompletely()
        {
            var numbers = Enumerable.Range(0, 6_000).ToArray();
            var expected = new StringBuilder("<ArrayOfInt32>");
            foreach (var number in numbers)
            {
                expected.Append("<Int32>").Append(number).Append("</Int32>");
            }

            expected.Append("</ArrayOfInt32>");
            using var stream = new MemoryStream();
            await XmlSerializer.SerializeAsyncEnumerable(stream, Values(numbers), Bare);
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be(expected.ToString());
            stream.Position = 0;
            (await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<int>(stream, Bare))).Should().Equal(numbers);
        }

        [TestMethod]
        public async Task AsyncEnumerable_Cancellation_StopsWritingAndReading()
        {
            using var cancelled = new CancellationTokenSource();
            await cancelled.CancelAsync();
            using var stream = new MemoryStream();
            await FluentActions.Awaiting(() => XmlSerializer.SerializeAsyncEnumerable(stream, Values(1, 2), Bare, cancelled.Token))
                .Should().ThrowAsync<OperationCanceledException>();
            stream.Length.Should().Be(0);

            using var source = new MemoryStream("<ArrayOfInt32><Int32>1</Int32><Int32>2</Int32></ArrayOfInt32>"u8.ToArray());
            using var afterFirst = new CancellationTokenSource();
            var read = new List<int>();
            await FluentActions.Awaiting(async () =>
            {
                await foreach (var value in XmlSerializer.DeserializeAsyncEnumerable<int>(source, Bare, afterFirst.Token))
                {
                    read.Add(value);
                    await afterFirst.CancelAsync();
                }
            }).Should().ThrowAsync<OperationCanceledException>();
            read.Should().Equal(1);

            source.Position = 0;
            using var viaEnumerator = new CancellationTokenSource();
            read.Clear();
            await FluentActions.Awaiting(async () =>
            {
                await foreach (var value in XmlSerializer.DeserializeAsyncEnumerable<int>(source, Bare).WithCancellation(viaEnumerator.Token))
                {
                    read.Add(value);
                    await viaEnumerator.CancelAsync();
                }
            }).Should().ThrowAsync<OperationCanceledException>();
            read.Should().Equal(1);

            source.Position = 0;
            using var argument = new CancellationTokenSource();
            using var second = new CancellationTokenSource();
            read.Clear();
            await foreach (var value in XmlSerializer.DeserializeAsyncEnumerable<int>(source, Bare, argument.Token).WithCancellation(second.Token))
            {
                read.Add(value);
            }

            read.Should().Equal(1, 2);
            source.Position = 0;
            read.Clear();
            await FluentActions.Awaiting(async () =>
            {
                await foreach (var value in XmlSerializer.DeserializeAsyncEnumerable<int>(source, Bare, argument.Token).WithCancellation(second.Token))
                {
                    read.Add(value);
                    await second.CancelAsync();
                }
            }).Should().ThrowAsync<OperationCanceledException>();
            read.Should().Equal(1);
        }

        [TestMethod]
        public async Task FailedWrite_OfADocumentThatFitsTheBuffer_LeavesTheDestinationUntouched()
        {
            const string message = "Named floating-point literals are not allowed when NumberHandling is Strict.";
            var buffer = new ArrayBufferWriter<byte>();
            FluentActions.Invoking(() => XmlSerializer.Serialize(buffer, double.NaN, StrictNumbers))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message + " Path: /Double.");
            buffer.WrittenCount.Should().Be(0);

            using var stream = new MemoryStream();
            FluentActions.Invoking(() => XmlSerializer.Serialize(stream, double.NaN, StrictNumbers))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message + " Path: /Double.");
            (await FluentActions.Awaiting(() => XmlSerializer.SerializeAsync(stream, double.NaN, StrictNumbers))
                .Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be(message + " Path: /Double.");
            (await FluentActions.Awaiting(() => XmlSerializer.SerializeAsyncEnumerable(stream, Values(1.5, double.NaN), StrictNumbers))
                .Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be(message + " Path: /ArrayOfDouble.");
            stream.Length.Should().Be(0);

            (await FluentActions.Awaiting(() => XmlSerializer.SerializeAsyncEnumerable(stream, Values(1.5, double.NaN), topLevelValues: true, StrictNumbers))
                .Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be(message + " Path: /Double.");
            Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<Double>1.5</Double>");

            XmlSerializer.Serialize(buffer, 2.5, StrictNumbers);
            Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<Double>2.5</Double>");
        }

        [TestMethod]
        public void SuccessiveCalls_WithDifferentOptions_EachUseTheirOwn()
        {
            var indented = new XmlSerializerOptions { NewLine = "\n", WriteIndented = true, XmlVersion = XmlVersion.Version11 };
            var small = new XmlSerializerOptions { DefaultBufferSize = 16, OmitXmlDeclaration = true };
            var text = new string('y', 5_000);
            for (var i = 0; i < 3; i++)
            {
                XmlSerializer.Serialize(1, indented).Should().Be("<?xml version=\"1.1\" encoding=\"utf-8\"?>\n<Int32>1</Int32>");
                XmlSerializer.Serialize(2, Bare).Should().Be("<Int32>2</Int32>");
                XmlSerializer.Serialize(text, small).Should().Be("<String>" + text + "</String>");
                using var stream = new MemoryStream();
                XmlSerializer.Serialize(stream, 3, indented);
                Encoding.UTF8.GetString(stream.ToArray()).Should().Be("<?xml version=\"1.1\" encoding=\"utf-8\"?>\n<Int32>3</Int32>");
                var buffer = new ArrayBufferWriter<byte>();
                XmlSerializer.Serialize(buffer, 4, Bare);
                Encoding.UTF8.GetString(buffer.WrittenSpan).Should().Be("<Int32>4</Int32>");
            }
        }

        [TestMethod]
        public void Converter_ThatCallsTheSerializerAgain_GetsItsOwnWriter()
        {
            const string inner = "&lt;Int32>5&lt;/Int32>";
            var expected = "<Envelope>" + inner + "|" + inner + "</Envelope>";
            for (var i = 0; i < 2; i++)
            {
                XmlSerializer.Serialize(new Envelope { Content = 5 }, Bare).Should().Be(expected);
                XmlSerializer.Deserialize<Envelope>(expected, Bare).Content.Should().Be(5);
                XmlSerializer.Serialize(6, Bare).Should().Be("<Int32>6</Int32>");
            }
        }

        #endregion

        #region Private Methods

        private static async Task<List<T>> ToListAsync<T>(IAsyncEnumerable<T> values)
        {
            var list = new List<T>();
            await foreach (var value in values)
            {
                list.Add(value);
            }

            return list;
        }

        private static async IAsyncEnumerable<T> Values<T>(params T[] values)
        {
            foreach (var value in values)
            {
                await Task.Yield();
                yield return value;
            }
        }

        #endregion

    }

}
