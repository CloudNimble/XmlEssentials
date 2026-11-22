using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.Text.Xml.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Every public <see cref="XmlSerializer"/> overload, for each source and destination, produces the same
    /// document or value whether it is given options, type info, a context, or a boxed value and its type.
    /// </summary>
    [TestClass]
    public class XmlSerializerOverloadTests
    {

        #region Fields

        private const string Xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Int32>42</Int32>";
        private static readonly XmlSerializerContext Context = new PrimitiveXmlContext();
        private static readonly XmlTypeInfo<int> TypeInfo = XmlSerializerOptions.Default.GetTypeInfo<int>();
        private static readonly byte[] Utf8 = Encoding.UTF8.GetBytes(Xml);

        #endregion

        #region Public Methods

        [TestMethod]
        public void Serialize_ToString()
        {
            XmlSerializer.Serialize(42).Should().Be(Xml);
            XmlSerializer.Serialize(42, TypeInfo).Should().Be(Xml);
            XmlSerializer.Serialize(42, Context).Should().Be(Xml);
            XmlSerializer.Serialize((object)42, typeof(int)).Should().Be(Xml);
            XmlSerializer.Serialize((object)42, typeof(int), Context).Should().Be(Xml);
        }

        [TestMethod]
        public void Serialize_ToUtf8Bytes()
        {
            XmlSerializer.SerializeToUtf8Bytes(42).Should().Equal(Utf8);
            XmlSerializer.SerializeToUtf8Bytes(42, TypeInfo).Should().Equal(Utf8);
            XmlSerializer.SerializeToUtf8Bytes(42, Context).Should().Equal(Utf8);
            XmlSerializer.SerializeToUtf8Bytes((object)42, typeof(int)).Should().Equal(Utf8);
            XmlSerializer.SerializeToUtf8Bytes((object)42, typeof(int), Context).Should().Equal(Utf8);
        }

        [TestMethod]
        public void Serialize_ToWriter_WritesOneElementAndLeavesTheDocumentToTheCaller()
        {
            const string element = "<r><Int32>42</Int32></r>";
            ToWriter(writer => XmlSerializer.Serialize(writer, 42)).Should().Be(element);
            ToWriter(writer => XmlSerializer.Serialize(writer, 42, TypeInfo)).Should().Be(element);
            ToWriter(writer => XmlSerializer.Serialize(writer, 42, Context)).Should().Be(element);
            ToWriter(writer => XmlSerializer.Serialize(writer, (object)42, typeof(int))).Should().Be(element);
            ToWriter(writer => XmlSerializer.Serialize(writer, (object)42, typeof(int), Context)).Should().Be(element);
        }

        [TestMethod]
        public void Serialize_ToStream()
        {
            ToStream(stream => XmlSerializer.Serialize(stream, 42)).Should().Be(Xml);
            ToStream(stream => XmlSerializer.Serialize(stream, 42, TypeInfo)).Should().Be(Xml);
            ToStream(stream => XmlSerializer.Serialize(stream, 42, Context)).Should().Be(Xml);
            ToStream(stream => XmlSerializer.Serialize(stream, (object)42, typeof(int))).Should().Be(Xml);
            ToStream(stream => XmlSerializer.Serialize(stream, (object)42, typeof(int), Context)).Should().Be(Xml);
        }

        [TestMethod]
        public void Serialize_ToBufferWriter()
        {
            ToBuffer(buffer => XmlSerializer.Serialize(buffer, 42)).Should().Be(Xml);
            ToBuffer(buffer => XmlSerializer.Serialize(buffer, 42, TypeInfo)).Should().Be(Xml);
            ToBuffer(buffer => XmlSerializer.Serialize(buffer, 42, Context)).Should().Be(Xml);
            ToBuffer(buffer => XmlSerializer.Serialize(buffer, (object)42, typeof(int))).Should().Be(Xml);
            ToBuffer(buffer => XmlSerializer.Serialize(buffer, (object)42, typeof(int), Context)).Should().Be(Xml);
        }

        [TestMethod]
        public async Task SerializeAsync_ToStream()
        {
            (await ToStreamAsync(stream => XmlSerializer.SerializeAsync(stream, 42))).Should().Be(Xml);
            (await ToStreamAsync(stream => XmlSerializer.SerializeAsync(stream, 42, TypeInfo))).Should().Be(Xml);
            (await ToStreamAsync(stream => XmlSerializer.SerializeAsync(stream, 42, Context))).Should().Be(Xml);
            (await ToStreamAsync(stream => XmlSerializer.SerializeAsync(stream, (object)42, typeof(int)))).Should().Be(Xml);
            (await ToStreamAsync(stream => XmlSerializer.SerializeAsync(stream, (object)42, typeof(int), Context))).Should().Be(Xml);
        }

        [TestMethod]
        public async Task Serialize_ToPipe()
        {
            (await ToPipeAsync(pipe =>
            {
                XmlSerializer.Serialize(pipe, 42);
                return pipe.FlushAsync().AsTask();
            })).Should().Be(Xml);
            (await ToPipeAsync(pipe =>
            {
                XmlSerializer.Serialize(pipe, 42, TypeInfo);
                return pipe.FlushAsync().AsTask();
            })).Should().Be(Xml);
            (await ToPipeAsync(pipe => XmlSerializer.SerializeAsync(pipe, 42))).Should().Be(Xml);
        }

        [TestMethod]
        public void Serialize_ToDocument()
        {
            using var fromOptions = XmlSerializer.SerializeToDocument(42);
            fromOptions.RootElement.GetRawXml().Should().Be("<Int32>42</Int32>");
            using var fromTypeInfo = XmlSerializer.SerializeToDocument(42, TypeInfo);
            fromTypeInfo.RootElement.GetRawXml().Should().Be("<Int32>42</Int32>");
            using var fromContext = XmlSerializer.SerializeToDocument(42, Context);
            fromContext.RootElement.GetRawXml().Should().Be("<Int32>42</Int32>");
            using var fromObject = XmlSerializer.SerializeToDocument((object)42, typeof(int));
            fromObject.RootElement.GetRawXml().Should().Be("<Int32>42</Int32>");
            using var fromObjectContext = XmlSerializer.SerializeToDocument((object)42, typeof(int), Context);
            fromObjectContext.RootElement.GetRawXml().Should().Be("<Int32>42</Int32>");
        }

        [TestMethod]
        public void Serialize_ToElement()
        {
            XmlSerializer.SerializeToElement(42).GetRawXml().Should().Be("<Int32>42</Int32>");
            XmlSerializer.SerializeToElement(42, TypeInfo).GetRawXml().Should().Be("<Int32>42</Int32>");
            XmlSerializer.SerializeToElement(42, Context).GetRawXml().Should().Be("<Int32>42</Int32>");
            XmlSerializer.SerializeToElement((object)42, typeof(int)).GetRawXml().Should().Be("<Int32>42</Int32>");
            XmlSerializer.SerializeToElement((object)42, typeof(int), Context).GetRawXml().Should().Be("<Int32>42</Int32>");
        }

        [TestMethod]
        public void Serialize_ToNode()
        {
            XmlSerializer.SerializeToNode(42)!.ToString().Should().Be("<Int32>42</Int32>");
            XmlSerializer.SerializeToNode(42, TypeInfo)!.ToString().Should().Be("<Int32>42</Int32>");
            XmlSerializer.SerializeToNode(42, Context)!.ToString().Should().Be("<Int32>42</Int32>");
            XmlSerializer.SerializeToNode((object)42, typeof(int))!.ToString().Should().Be("<Int32>42</Int32>");
            XmlSerializer.SerializeToNode((object)42, typeof(int), Context)!.ToString().Should().Be("<Int32>42</Int32>");
        }

        [TestMethod]
        public void Deserialize_FromString()
        {
            XmlSerializer.Deserialize<int>(Xml).Should().Be(42);
            XmlSerializer.Deserialize(Xml, TypeInfo).Should().Be(42);
            XmlSerializer.Deserialize<int>(Xml, Context).Should().Be(42);
            XmlSerializer.Deserialize(Xml, typeof(int)).Should().Be(42);
            XmlSerializer.Deserialize(Xml, typeof(int), Context).Should().Be(42);
        }

        [TestMethod]
        public void Deserialize_FromChars()
        {
            XmlSerializer.Deserialize<int>(Xml.AsSpan()).Should().Be(42);
            XmlSerializer.Deserialize(Xml.AsSpan(), TypeInfo).Should().Be(42);
            XmlSerializer.Deserialize<int>(Xml.AsSpan(), Context).Should().Be(42);
            XmlSerializer.Deserialize(Xml.AsSpan(), typeof(int)).Should().Be(42);
            XmlSerializer.Deserialize(Xml.AsSpan(), typeof(int), Context).Should().Be(42);
            XmlSerializer.Deserialize<string>("<String>é☃𝄞</String>".AsSpan()).Should().Be("é☃𝄞");
        }

        [TestMethod]
        public void Deserialize_FromUtf8()
        {
            XmlSerializer.Deserialize<int>(Utf8.AsSpan()).Should().Be(42);
            XmlSerializer.Deserialize(Utf8.AsSpan(), TypeInfo).Should().Be(42);
            XmlSerializer.Deserialize<int>(Utf8.AsSpan(), Context).Should().Be(42);
            XmlSerializer.Deserialize(Utf8.AsSpan(), typeof(int)).Should().Be(42);
            XmlSerializer.Deserialize(Utf8.AsSpan(), typeof(int), Context).Should().Be(42);
        }

        [TestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void Deserialize_FromSequence(bool segmented)
        {
            var sequence = new ReadOnlySequence<byte>(Utf8);
            if (segmented)
            {
                var first = new ByteSequenceSegment(Utf8.AsMemory(0, 10), 0);
                var last = first.Append(Utf8.AsMemory(10, 31)).Append(Utf8.AsMemory(41, 5)).Append(Utf8.AsMemory(46));
                sequence = new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length);
            }

            XmlSerializer.Deserialize<int>(sequence).Should().Be(42);
            XmlSerializer.Deserialize(sequence, TypeInfo).Should().Be(42);
            XmlSerializer.Deserialize<int>(sequence, Context).Should().Be(42);
            XmlSerializer.Deserialize(sequence, typeof(int)).Should().Be(42);
            XmlSerializer.Deserialize(sequence, typeof(int), Context).Should().Be(42);
        }

        [TestMethod]
        public void Deserialize_FromReader()
        {
            var reader = new Utf8XmlReader(Utf8);
            XmlSerializer.Deserialize<int>(ref reader).Should().Be(42);
            reader = new Utf8XmlReader(Utf8);
            XmlSerializer.Deserialize(ref reader, TypeInfo).Should().Be(42);
            reader = new Utf8XmlReader(Utf8);
            XmlSerializer.Deserialize<int>(ref reader, Context).Should().Be(42);
            reader = new Utf8XmlReader(Utf8);
            XmlSerializer.Deserialize(ref reader, typeof(int)).Should().Be(42);
            reader = new Utf8XmlReader(Utf8);
            XmlSerializer.Deserialize(ref reader, typeof(int), Context).Should().Be(42);
        }

        [TestMethod]
        public void Deserialize_FromStream()
        {
            XmlSerializer.Deserialize<int>(new MemoryStream(Utf8)).Should().Be(42);
            XmlSerializer.Deserialize(new MemoryStream(Utf8), TypeInfo).Should().Be(42);
            XmlSerializer.Deserialize<int>(new MemoryStream(Utf8), Context).Should().Be(42);
            XmlSerializer.Deserialize(new MemoryStream(Utf8), typeof(int)).Should().Be(42);
            XmlSerializer.Deserialize(new MemoryStream(Utf8), typeof(int), Context).Should().Be(42);
        }

        [TestMethod]
        public async Task DeserializeAsync_FromStream()
        {
            (await XmlSerializer.DeserializeAsync<int>(new MemoryStream(Utf8))).Should().Be(42);
            (await XmlSerializer.DeserializeAsync(new MemoryStream(Utf8), TypeInfo)).Should().Be(42);
            (await XmlSerializer.DeserializeAsync<int>(new MemoryStream(Utf8), Context)).Should().Be(42);
            (await XmlSerializer.DeserializeAsync(new MemoryStream(Utf8), typeof(int))).Should().Be(42);
            (await XmlSerializer.DeserializeAsync(new MemoryStream(Utf8), typeof(int), Context)).Should().Be(42);
        }

        [TestMethod]
        public async Task DeserializeAsync_FromPipe()
        {
            (await XmlSerializer.DeserializeAsync<int>(CompletedPipe())).Should().Be(42);
            (await XmlSerializer.DeserializeAsync(CompletedPipe(), TypeInfo)).Should().Be(42);
        }

        [TestMethod]
        public void Deserialize_FromDocument()
        {
            using var document = XmlDocument.Parse(Xml);
            XmlSerializer.Deserialize<int>(document).Should().Be(42);
            XmlSerializer.Deserialize(document, TypeInfo).Should().Be(42);
            XmlSerializer.Deserialize<int>(document, Context).Should().Be(42);
            XmlSerializer.Deserialize(document, typeof(int)).Should().Be(42);
            XmlSerializer.Deserialize(document, typeof(int), Context).Should().Be(42);
        }

        [TestMethod]
        public void Deserialize_FromElement()
        {
            using var document = XmlDocument.Parse("<r><Int32>42</Int32></r>");
            var element = document.RootElement.GetChild("Int32");
            XmlSerializer.Deserialize<int>(element).Should().Be(42);
            XmlSerializer.Deserialize(element, TypeInfo).Should().Be(42);
            XmlSerializer.Deserialize<int>(element, Context).Should().Be(42);
            XmlSerializer.Deserialize(element, typeof(int)).Should().Be(42);
            XmlSerializer.Deserialize(element, typeof(int), Context).Should().Be(42);
        }

        [TestMethod]
        public void Deserialize_FromElement_KeepsNamespacesDeclaredOnItsAncestors()
        {
            var namespaced = new XmlSerializerOptions { DefaultNamespace = "urn:x" };
            using var document = XmlDocument.Parse("<r xmlns:p=\"urn:x\"><p:Int32>42</p:Int32></r>");
            XmlSerializer.Deserialize<int>(document.RootElement.GetChild("Int32", "urn:x"), namespaced).Should().Be(42);
        }

        [TestMethod]
        public void Deserialize_FromSequence_SplitInsideTextAndReferences()
        {
            var utf8 = "<String>a&amp;b<!--c-->cd&#233;</String>"u8.ToArray();
            for (var split = 1; split < utf8.Length; split++)
            {
                var first = new ByteSequenceSegment(utf8.AsMemory(0, split), 0);
                var last = first.Append(utf8.AsMemory(split));
                XmlSerializer.Deserialize<string>(new ReadOnlySequence<byte>(first, 0, last, last.Memory.Length)).Should().Be("a&bcdé");
            }
        }

        [TestMethod]
        public void Deserialize_FromDom_KeepsCharactersOfEitherXmlVersion()
        {
            var control = ((char)1).ToString();
            var nextLine = ((char)0x85).ToString();
            const string version11 = "<?xml version=\"1.1\"?><String>a&#x1;b&#x85;c</String>";
            using var document11 = XmlDocument.Parse(version11);
            XmlSerializer.Deserialize<string>(version11).Should().Be("a" + control + "b" + nextLine + "c");
            XmlSerializer.Deserialize<string>(document11).Should().Be("a" + control + "b" + nextLine + "c");
            XmlSerializer.Deserialize<string>(document11.RootElement).Should().Be("a" + control + "b" + nextLine + "c");
            XmlSerializer.Deserialize<string>(document11.RootElement.CloneNode()).Should().Be("a" + control + "b" + nextLine + "c");

            const string version10 = "<String>a&#x85;b&#x2028;c\u007Fd</String>";
            var expected = "a" + nextLine + "b" + ((char)0x2028) + "c" + ((char)0x7F) + "d";
            using var document10 = XmlDocument.Parse(version10);
            XmlSerializer.Deserialize<string>(version10).Should().Be(expected);
            XmlSerializer.Deserialize<string>(document10).Should().Be(expected);
            XmlSerializer.Deserialize<string>(document10.RootElement).Should().Be(expected);
            XmlSerializer.Deserialize<string>(document10.RootElement.CloneNode()).Should().Be(expected);
        }

        [TestMethod]
        public void Deserialize_FromNode()
        {
            var node = XmlNode.Parse(Xml)!;
            XmlSerializer.Deserialize<int>(node).Should().Be(42);
            XmlSerializer.Deserialize(node, TypeInfo).Should().Be(42);
            XmlSerializer.Deserialize<int>(node, Context).Should().Be(42);
            XmlSerializer.Deserialize(node, typeof(int)).Should().Be(42);
            XmlSerializer.Deserialize(node, typeof(int), Context).Should().Be(42);
        }

        [TestMethod]
        public void BoxedValue_OfAnotherType_IsRejected()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize("text", typeof(int))).Should().ThrowExactly<InvalidCastException>();
        }

        [TestMethod]
        public async Task NullArguments_AreNamed()
        {
            Named("xmlTypeInfo", () => XmlSerializer.Serialize(42, (XmlTypeInfo<int>)null!));
            Named("xmlTypeInfo", () => XmlSerializer.SerializeToUtf8Bytes(42, (XmlTypeInfo<int>)null!));
            Named("xmlTypeInfo", () => XmlSerializer.SerializeToDocument(42, (XmlTypeInfo<int>)null!));
            Named("xmlTypeInfo", () => XmlSerializer.SerializeToNode(42, (XmlTypeInfo<int>)null!));
            Named("xmlTypeInfo", () => XmlSerializer.Deserialize(Xml, (XmlTypeInfo<int>)null!));
            Named("context", () => XmlSerializer.Serialize(42, (XmlSerializerContext)null!));
            Named("context", () => XmlSerializer.Deserialize(Xml, typeof(int), (XmlSerializerContext)null!));
            Named("inputType", () => XmlSerializer.Serialize(42, null!, XmlSerializerOptions.Default));
            Named("returnType", () => XmlSerializer.Deserialize(Xml, null!, Context));
            Named("xml", () => XmlSerializer.Deserialize<int>((string)null!));
            Named("writer", () => XmlSerializer.Serialize((Utf8XmlWriter)null!, 42));
            Named("utf8Xml", () => XmlSerializer.Serialize((Stream)null!, 42));
            Named("bufferWriter", () => XmlSerializer.Serialize((IBufferWriter<byte>)null!, 42));
            Named("utf8Xml", () => XmlSerializer.Deserialize<int>((Stream)null!));
            Named("document", () => XmlSerializer.Deserialize<int>((XmlDocument)null!));
            Named("node", () => XmlSerializer.Deserialize<int>((XmlNode)null!));
            (await FluentActions.Awaiting(() => XmlSerializer.SerializeAsync((Stream)null!, 42)).Should().ThrowExactlyAsync<ArgumentNullException>())
                .Which.ParamName.Should().Be("utf8Xml");
            (await FluentActions.Awaiting(() => XmlSerializer.SerializeAsync((PipeWriter)null!, 42)).Should().ThrowExactlyAsync<ArgumentNullException>())
                .Which.ParamName.Should().Be("utf8Xml");
            (await FluentActions.Awaiting(() => XmlSerializer.SerializeAsync(new MemoryStream(), 42, (XmlTypeInfo<int>)null!)).Should().ThrowExactlyAsync<ArgumentNullException>())
                .Which.ParamName.Should().Be("xmlTypeInfo");
            (await FluentActions.Awaiting(() => XmlSerializer.DeserializeAsync<int>((Stream)null!).AsTask()).Should().ThrowExactlyAsync<ArgumentNullException>())
                .Which.ParamName.Should().Be("utf8Xml");
            (await FluentActions.Awaiting(() => XmlSerializer.DeserializeAsync<int>((PipeReader)null!).AsTask()).Should().ThrowExactlyAsync<ArgumentNullException>())
                .Which.ParamName.Should().Be("utf8Xml");
            (await FluentActions.Awaiting(() => XmlSerializer.DeserializeAsync(new Pipe().Reader, (XmlTypeInfo<int>)null!).AsTask()).Should().ThrowExactlyAsync<ArgumentNullException>())
                .Which.ParamName.Should().Be("xmlTypeInfo");
        }

        #endregion

        #region Private Methods

        private static PipeReader CompletedPipe()
        {
            var pipe = new Pipe();
            pipe.Writer.Write(Utf8);
            pipe.Writer.Complete();
            return pipe.Reader;
        }

        private static void Named(string parameter, Action action)
        {
            action.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be(parameter);
        }

        private static string ToBuffer(Action<ArrayBufferWriter<byte>> write)
        {
            var buffer = new ArrayBufferWriter<byte>();
            write(buffer);
            return Encoding.UTF8.GetString(buffer.WrittenSpan);
        }

        private static async Task<string> ToPipeAsync(Func<PipeWriter, Task> write)
        {
            var pipe = new Pipe();
            await write(pipe.Writer);
            await pipe.Writer.CompleteAsync();
            var result = await pipe.Reader.ReadAsync();
            return Encoding.UTF8.GetString(result.Buffer);
        }

        private static string ToStream(Action<MemoryStream> write)
        {
            using var stream = new MemoryStream();
            write(stream);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static async Task<string> ToStreamAsync(Func<MemoryStream, Task> write)
        {
            using var stream = new MemoryStream();
            await write(stream);
            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static string ToWriter(Action<Utf8XmlWriter> write)
        {
            return XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("r");
                write(writer);
                writer.WriteEndElement();
            });
        }

        #endregion

    }

}
