using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Objects read from a stream a piece at a time. A value keeps what it has read when a block of input ends
    /// in the middle of it and goes on from there, so it must come out the same wherever the stream is cut, and
    /// the same as the value read from a document that is all there.
    /// </summary>
    [TestClass]
    public class XmlSerializerResumableReadTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };
        private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";

        /// <summary>
        /// Three orders. Between them they have every kind of member, in order and out of it, with text in one
        /// piece and in several, nil members and items, empty elements, and elements no member takes.
        /// </summary>
        private static readonly string[] Orders =
        [
            "<ResumeOrder Id=\"1\"><Region>north &amp; east</Region><Customer>Ada</Customer><Total>12.50</Total>"
                + "<ShipTo><City>Kalamazoo</City><Zip>49007</Zip></ShipTo>"
                + "<Lines><ResumeLine><Sku>A-1</Sku><Quantity>2</Quantity></ResumeLine><ResumeLine><Quantity>3</Quantity><Sku>B&lt;2&gt;</Sku></ResumeLine></Lines>"
                + "<Tags>retail</Tags><Tags>net-30</Tags><Codes><int>7</int><int>8</int></Codes>"
                + "<Stops><City>Portage</City></Stops><Stops><Zip>49002</Zip><City>Oshtemo</City></Stops></ResumeOrder>",
            "<ResumeOrder Id=\"2\">\r\n  <!-- out of order, with text in pieces -->\r\n  <Total> 0.75 </Total>\r\n  <Unknown a=\"1\"><Deep><Deeper>x</Deeper></Deep>tail</Unknown>\r\n"
                + "  <Customer>Grace <![CDATA[<Hopper>]]><!-- a comment --> &amp; co<?pi data?></Customer>\r\n  <Computed>ignored<b/></Computed>\r\n  <Legacy>dropped<x/></Legacy>\r\n"
                + "  <Region><![CDATA[]]></Region>\r\n  <ShipTo" + Nil + "/>\r\n  <Lines" + Nil + "></Lines>\r\n  <Tags/>\r\n  <Tags" + Nil + "/>\r\n"
                + "  <Stops" + Nil + "> </Stops>\r\n  <Stops/>\r\n  <Codes/>\r\n</ResumeOrder>",
            "<ResumeOrder Id=\"3\"><Region>south</Region><Customer" + Nil + "/><ShipTo/><Lines><ResumeLine><Sku>C</Sku><Quantity>1</Quantity><Extra/></ResumeLine></Lines>"
                + "<Codes> <int>1</int> <!-- between items --><![CDATA[not an item]]> <int>2</int> </Codes><Unknown/>stray text<?pi between members?></ResumeOrder>",
        ];

        #endregion

        #region Public Methods

        [TestMethod]
        public async Task Objects_ReadFromAStream_AreTheSame_WhereverTheStreamIsCut()
        {
            // What each order is when it is read from a document of its own, written back.
            var expected = Orders.Select(order => XmlSerializer.Serialize(XmlSerializer.Deserialize<ResumeOrder>(order, Bare), Bare)).ToArray();
            expected[0].Should().Be(
                "<ResumeOrder Id=\"1\"><Codes><int>7</int><int>8</int></Codes><Computed>computed</Computed><Customer>Ada</Customer><Legacy>kept</Legacy>"
                + "<Lines><ResumeLine><Sku>A-1</Sku><Quantity>2</Quantity></ResumeLine><ResumeLine><Sku>B&lt;2></Sku><Quantity>3</Quantity></ResumeLine></Lines>"
                + "<Region>north &amp; east</Region><ShipTo><City>Kalamazoo</City><Zip>49007</Zip></ShipTo>"
                + "<Stops><City>Portage</City><Zip" + Nil + "/></Stops><Stops><City>Oshtemo</City><Zip>49002</Zip></Stops>"
                + "<Tags>retail</Tags><Tags>net-30</Tags><Total>12.50</Total></ResumeOrder>");

            var utf8 = Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?>\n<ArrayOfResumeOrder>\n" + string.Join("\n", Orders) + "\n</ArrayOfResumeOrder>\n");
            foreach (var bufferSize in new[] { 16, 4096 })
            {
                var options = new XmlSerializerOptions { DefaultBufferSize = bufferSize };
                for (var chunk = 1; chunk <= utf8.Length; chunk += chunk < 64 ? 1 : 97)
                {
                    var values = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<ResumeOrder>(new TrickleStream(utf8, chunk), options));
                    values.Select(value => XmlSerializer.Serialize(value, Bare)).Should().Equal(expected, "the stream gives {0} bytes at a time into a buffer of {1}", chunk, bufferSize);
                }
            }
        }

        [TestMethod]
        [DataRow("<ResumeOrder><Region>r</Region><Total>x</Total></ResumeOrder>", DisplayName = "a member that is not a value of its type")]
        [DataRow("<ResumeOrder><Region>r</Region><ShipTo><City>c</City><City>d</City></ShipTo></ResumeOrder>", DisplayName = "a member of an object member that appears twice")]
        [DataRow("<ResumeOrder><Region>r</Region><Lines><ResumeLine><Sku>s</Sku><Quantity>1</Quantity></ResumeLine><ResumeLine><Sku>s</Sku><Quantity>q</Quantity></ResumeLine></Lines></ResumeOrder>", DisplayName = "an item in a wrapper whose member is not a value of its type")]
        [DataRow("<ResumeOrder><Region>r</Region><Codes><int>1</int><int>two</int></Codes></ResumeOrder>", DisplayName = "a number in a wrapper that is not a number")]
        [DataRow("<ResumeOrder><Region>r</Region><Stops><City>a</City></Stops><Stops><Bogus/></Stops></ResumeOrder>", DisplayName = "an element no member takes, in an item with no wrapper")]
        [DataRow("<ResumeOrder><Customer>c</Customer></ResumeOrder>", DisplayName = "a required member that is missing")]
        [DataRow("<ResumeOrder><Region>r</Region><![CDATA[text]]></ResumeOrder>", DisplayName = "a CDATA section between the members")]
        [DataRow("<ResumeOrder><Region>r</Region><Total><b>1</b></Total></ResumeOrder>", DisplayName = "an element inside a value")]
        [DataRow("<ResumeOrder><Region>r</Region><Total xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/></ResumeOrder>", DisplayName = "a nil element for a value that cannot be null")]
        [DataRow("<ResumeOrder xmlns:r=\"urn:CloudNimble.Text.Xml:refs\" r:ref=\"7\"><Region>r</Region></ResumeOrder>", DisplayName = "a reference, with nothing tracking references")]
        [DataRow("<ResumeOrder><Region>r</Region><Region>s</Region></ResumeOrder>", DisplayName = "a member that appears twice")]
        public async Task Objects_ReadFromAStream_ReportAnErrorTheSameWay_WhereverTheStreamIsCut(string order)
        {
            // The same value read from a document that is all there says what the error is and where. A stream
            // must say the same, with the same path, however it is cut.
            var options = new XmlSerializerOptions { AllowDuplicateProperties = false, DefaultBufferSize = 16, UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow };
            var whole = FluentActions.Invoking(() => XmlSerializer.Deserialize<ResumeOrder>(order, options)).Should().ThrowExactly<XmlException>().Which;

            // In the stream the value follows a start tag of 20 bytes on the same line, and nothing else differs.
            var expected = whole.Message.Replace($"BytePositionInLine: {whole.LinePosition}.", $"BytePositionInLine: {whole.LinePosition + 20}.");
            var utf8 = Encoding.UTF8.GetBytes("<ArrayOfResumeOrder>" + order + "</ArrayOfResumeOrder>");
            foreach (var chunk in new[] { 1, 2, 3, 5, 8, 13, utf8.Length })
            {
                var read = async () => await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<ResumeOrder>(new TrickleStream(utf8, chunk), topLevelValues: false, options));
                var thrown = (await read.Should().ThrowExactlyAsync<XmlException>()).Which;
                thrown.Path.Should().Be(whole.Path, "the stream gives {0} bytes at a time", chunk);
                thrown.Message.Should().Be(expected, "the stream gives {0} bytes at a time", chunk);
            }
        }

        [TestMethod]
        public async Task Objects_ReadFromAStream_PassOnAnExceptionThatIsNotAboutTheXml_AndReadTheNextStreamAfresh()
        {
            // A setter refuses a value. The exception is the setter's own, and nothing of the value that failed is
            // left behind for the next read.
            var refused = Encoding.UTF8.GetBytes("<ArrayOfResumeRefuser><ResumeRefuser><Name>yes</Name></ResumeRefuser><ResumeRefuser><Name>no</Name></ResumeRefuser></ArrayOfResumeRefuser>");
            var accepted = Encoding.UTF8.GetBytes("<ArrayOfResumeRefuser><ResumeRefuser><Name>yes</Name></ResumeRefuser><ResumeRefuser><Name>also</Name></ResumeRefuser></ArrayOfResumeRefuser>");
            var options = new XmlSerializerOptions { DefaultBufferSize = 16 };
            foreach (var chunk in new[] { 1, 7, refused.Length })
            {
                var read = async () => await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<ResumeRefuser>(new TrickleStream(refused, chunk), options));
                (await read.Should().ThrowExactlyAsync<InvalidOperationException>()).Which.Message.Should().Be("The name is refused.");
                var values = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<ResumeRefuser>(new TrickleStream(accepted, chunk), options));
                values.Select(value => value!.Name).Should().Equal("yes", "also");
            }
        }

        [TestMethod]
        public async Task DerivedObjects_ReadFromAStream_AreTheTypeTheDocumentNames_WhereverTheStreamIsCut()
        {
            // A type with derived types is not read a piece at a time, and its values must still be the types
            // their discriminators name.
            var utf8 = Encoding.UTF8.GetBytes("<ArrayOfKindProduct><KindProduct kind=\"book\"><Title>b</Title><Author>a</Author></KindProduct>"
                + "<KindProduct><Title>p</Title></KindProduct><KindProduct kind=\"dvd\"><Title>d</Title></KindProduct></ArrayOfKindProduct>");
            foreach (var chunk in new[] { 1, 5, 16, utf8.Length })
            {
                var values = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<KindProduct>(new TrickleStream(utf8, chunk), new XmlSerializerOptions { DefaultBufferSize = 16 }));
                values.Select(value => value!.GetType()).Should().Equal([typeof(KindBook), typeof(KindProduct), typeof(KindDvd)], "the stream gives {0} bytes at a time", chunk);
                values.Select(value => value!.Title).Should().Equal(["b", "p", "d"], "the stream gives {0} bytes at a time", chunk);
                ((KindBook)values[0]!).Author.Should().Be("a");
            }

            // A type whose derived types are named by the element is read by the name of each element.
            var shapes = Encoding.UTF8.GetBytes("<ArrayOfNamedShape><NamedCircle><Label>c</Label><Radius>2</Radius></NamedCircle><NamedSquare><Label>s</Label></NamedSquare></ArrayOfNamedShape>");
            foreach (var chunk in new[] { 1, 5, 16, shapes.Length })
            {
                var values = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<NamedShape>(new TrickleStream(shapes, chunk), new XmlSerializerOptions { DefaultBufferSize = 16 }));
                values.Select(value => value!.GetType()).Should().Equal([typeof(NamedCircle), typeof(NamedSquare)], "the stream gives {0} bytes at a time", chunk);
                values.Select(value => value!.Label).Should().Equal(["c", "s"], "the stream gives {0} bytes at a time", chunk);
                ((NamedCircle)values[0]!).Radius.Should().Be(2);
            }

            // A discriminator that names no declared type is refused, as it is in a document that is all there.
            var unknown = Encoding.UTF8.GetBytes("<ArrayOfKindProduct><KindProduct kind=\"toy\"><Title>t</Title></KindProduct></ArrayOfKindProduct>");
            var read = async () => await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<KindProduct>(new TrickleStream(unknown, 3), new XmlSerializerOptions { DefaultBufferSize = 16 }));
            var whole = FluentActions.Invoking(() => XmlSerializer.Deserialize<KindProduct>("<KindProduct kind=\"toy\"><Title>t</Title></KindProduct>")).Should().ThrowExactly<XmlException>().Which;
            (await read.Should().ThrowExactlyAsync<XmlException>()).Which.Message.Should().Be(whole.Message.Replace($"BytePositionInLine: {whole.LinePosition}.", $"BytePositionInLine: {whole.LinePosition + 20}."));
        }

        [TestMethod]
        public async Task Trees_ReadFromAStream_AreTheSame_WhereverTheStreamIsCut()
        {
            // A type that holds values of its own type has a frame for each level, however deep the document goes.
            const string tree = "<ResumeTree><Name>root</Name><Trunk><Name>trunk</Name><Trunk><Name>deep</Name></Trunk></Trunk>"
                + "<Branches><Name>a</Name><Branches><Name>a1</Name></Branches><Branches><Name>a2</Name></Branches></Branches><Branches><Name>b</Name></Branches></ResumeTree>";
            var expected = XmlSerializer.Serialize(XmlSerializer.Deserialize<ResumeTree>(tree, Bare), Bare);
            var utf8 = Encoding.UTF8.GetBytes("<ArrayOfResumeTree>" + tree + tree + "</ArrayOfResumeTree>");
            for (var chunk = 1; chunk <= utf8.Length; chunk += 3)
            {
                var values = await ToListAsync(XmlSerializer.DeserializeAsyncEnumerable<ResumeTree>(new TrickleStream(utf8, chunk), new XmlSerializerOptions { DefaultBufferSize = 16 }));
                values.Select(value => XmlSerializer.Serialize(value, Bare)).Should().Equal([expected, expected], "the stream gives {0} bytes at a time", chunk);
            }
        }

        #endregion

        #region Private Methods

        private static async Task<List<T?>> ToListAsync<T>(IAsyncEnumerable<T?> values)
        {
            var list = new List<T?>();
            await foreach (var value in values)
            {
                list.Add(value);
            }

            return list;
        }

        #endregion

    }

}
