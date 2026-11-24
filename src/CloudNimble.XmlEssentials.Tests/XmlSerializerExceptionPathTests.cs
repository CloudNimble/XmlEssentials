using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Tests.POCOs.Paths;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="XmlException.Path"/>: where in the document an error was, for an exception that is thrown while
    /// the serializer reads or writes. It is the counterpart of <c>JsonException.Path</c>.
    /// </summary>
    /// <remarks>
    /// Each case gives the path, the line and the position in the line, and the message, which the path is not
    /// added to.
    /// </remarks>
    [TestClass]
    public class XmlSerializerExceptionPathTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        [DataRow("<PathOrder Id=\"x\"/>", "/PathOrder/@Id|1:20|The value is not a valid integer. Path: /PathOrder/@Id | LineNumber: 1 | BytePositionInLine: 20.", DisplayName = "an attribute")]
        [DataRow("<PathOrder><Lines><PathLine><Qty>1</Qty></PathLine><PathLine><Qty>x</Qty></PathLine></Lines></PathOrder>", "/PathOrder/Lines/PathLine[1]/Qty|1:74|The value is not a valid integer. Path: /PathOrder/Lines/PathLine[1]/Qty | LineNumber: 1 | BytePositionInLine: 74.", DisplayName = "an element of the second item in a wrapper")]
        [DataRow("<PathOrder><Lines><PathLine Row=\"x\"/></Lines></PathOrder>", "/PathOrder/Lines/PathLine[0]/@Row|1:38|The value is not a valid integer. Path: /PathOrder/Lines/PathLine[0]/@Row | LineNumber: 1 | BytePositionInLine: 38.", DisplayName = "an attribute of the first item in a wrapper")]
        [DataRow("<PathOrder><Extras><Qty>1</Qty></Extras><Extras><Qty>x</Qty></Extras></PathOrder>", "/PathOrder/Extras[1]/Qty|1:61|The value is not a valid integer. Path: /PathOrder/Extras[1]/Qty | LineNumber: 1 | BytePositionInLine: 61.", DisplayName = "an element of the second item with no wrapper")]
        [DataRow("<PathOrder><Counts><a>1</a><b>x</b></Counts></PathOrder>", "/PathOrder/Counts/b|1:36|The value is not a valid integer. Path: /PathOrder/Counts/b | LineNumber: 1 | BytePositionInLine: 36.", DisplayName = "an entry of a dictionary")]
        [DataRow("<PathOrder><Shapes><circle><Radius>1</Radius></circle><circle><Radius>x</Radius></circle></Shapes></PathOrder>", "/PathOrder/Shapes/circle[1]/Radius|1:81|The value is not a valid integer. Path: /PathOrder/Shapes/circle[1]/Radius | LineNumber: 1 | BytePositionInLine: 81.", DisplayName = "an element of an item of a derived type")]
        [DataRow("<PathOrder><Note Lang=\"en\">x</Note></PathOrder>", "/PathOrder/Note/text()|1:36|The value is not a valid integer. Path: /PathOrder/Note/text() | LineNumber: 1 | BytePositionInLine: 36.", DisplayName = "the text of an element")]
        [DataRow("<PathOrder>\n  <Lines>\n    <PathLine><Qty>1</Qty></PathLine>\n    <PathLine><Qty>2</Oops>", "/PathOrder/Lines/PathLine[1]/Qty|4:28|End tag does not match start tag. Path: /PathOrder/Lines/PathLine[1]/Qty | LineNumber: 4 | BytePositionInLine: 28.", DisplayName = "XML that is not well formed, on its fourth line")]
        [DataRow("<PathOrder><Odd/></PathOrder>", "/PathOrder/Odd|1:18|mine", DisplayName = "a converter that throws with a message of its own")]
        [DataRow("<PathOrder><Odd path=\"/given\"/></PathOrder>", "/given|2:3|mine", DisplayName = "a converter that throws with a path and a position of its own")]
        [DataRow("<PathOrder><Odds><PathOdd path=\"/given\"/></Odds></PathOrder>", "/given|2:3|mine", DisplayName = "an item whose converter throws with a path of its own")]
        [DataRow("<PathOrder><Shapes><circle><Odd path=\"/given\"/></circle></Shapes></PathOrder>", "/given|2:3|mine", DisplayName = "an item of a derived type whose converter throws with a path of its own")]
        public void Read_SetsThePathOfTheError(string xml, string expected)
        {
            Describe(() => XmlSerializer.Deserialize<PathOrder>(xml, Bare)).Should().Be(expected);
        }

        [TestMethod]
        public void Read_OfAnErrorThatIsNotInAMember_EndsThePathAtTheElement()
        {
            // The second Radius is not a value that failed to be read: the element it is in has two of them.
            var strict = new XmlSerializerOptions { AllowDuplicateProperties = false, OmitXmlDeclaration = true };
            Describe(() => XmlSerializer.Deserialize<PathOrder>("<PathOrder><Shapes><circle><Radius>1</Radius><Radius>2</Radius></circle></Shapes></PathOrder>", strict))
                .Should().Be("/PathOrder/Shapes/circle[0]|1:54|Duplicate property 'Radius'. Path: /PathOrder/Shapes/circle[0] | LineNumber: 1 | BytePositionInLine: 54.");
            Describe(() => XmlSerializer.Deserialize<PathOrder>("<PathOrder><Lines><PathLine><Qty>1</Qty><Qty>2</Qty></PathLine></Lines></PathOrder>", strict))
                .Should().Be("/PathOrder/Lines/PathLine[0]|1:46|Duplicate property 'Qty'. Path: /PathOrder/Lines/PathLine[0] | LineNumber: 1 | BytePositionInLine: 46.");
        }

        [TestMethod]
        public void Read_OfADocumentElementThatIsNotAnObject_StartsThePathThere()
        {
            Describe(() => XmlSerializer.Deserialize<int>("<Int32>x</Int32>", Bare)).Should().Be("/Int32|1:17|The value is not a valid integer. Path: /Int32 | LineNumber: 1 | BytePositionInLine: 17.");
            Describe(() => XmlSerializer.Deserialize<List<int>>("<ListOfInt32><Int32>1</Int32><Int32>x</Int32></ListOfInt32>", Bare)).Should().Be("/ListOfInt32/Int32[1]|1:46|The value is not a valid integer. Path: /ListOfInt32/Int32[1] | LineNumber: 1 | BytePositionInLine: 46.");
            Describe(() => XmlSerializer.Deserialize<Dictionary<string, int>>("<DictionaryOfStringInt32><a>x</a></DictionaryOfStringInt32>", Bare)).Should().Be("/DictionaryOfStringInt32/a|1:34|The value is not a valid integer. Path: /DictionaryOfStringInt32/a | LineNumber: 1 | BytePositionInLine: 34.");
            Describe(() => XmlSerializer.Deserialize<PathShape>("<circle><Radius>x</Radius></circle>", Bare)).Should().Be("/circle/Radius|1:27|The value is not a valid integer. Path: /circle/Radius | LineNumber: 1 | BytePositionInLine: 27.");
            Describe(() => XmlSerializer.Deserialize<Dictionary<int, int>>("<DictionaryOfInt32Int32><Key>1</Key><Value>x</Value></DictionaryOfInt32Int32>", Bare)).Should().Be("/DictionaryOfInt32Int32/Value|1:53|The value is not a valid integer. Path: /DictionaryOfInt32Int32/Value | LineNumber: 1 | BytePositionInLine: 53.");
            Describe(() => XmlSerializer.Deserialize<Dictionary<int, int>>("<DictionaryOfInt32Int32><Key>x</Key><Value>1</Value></DictionaryOfInt32Int32>", Bare)).Should().Be("/DictionaryOfInt32Int32/Key|1:37|The value is not a valid integer. Path: /DictionaryOfInt32Int32/Key | LineNumber: 1 | BytePositionInLine: 37.");
        }

        [TestMethod]
        public void Read_FromADocumentOrANode_SetsThePath()
        {
            using var document = XmlDocument.Parse("<PathOrder><Lines><PathLine><Qty>x</Qty></PathLine></Lines></PathOrder>");
            Describe(() => XmlSerializer.Deserialize<PathOrder>(document, Bare)).Should().Be("/PathOrder/Lines/PathLine[0]/Qty|1:79|The value is not a valid integer. Path: /PathOrder/Lines/PathLine[0]/Qty | LineNumber: 1 | BytePositionInLine: 79.");
            Describe(() => XmlSerializer.Deserialize<PathOrder>(document.RootElement, Bare)).Should().Be("/PathOrder/Lines/PathLine[0]/Qty|1:79|The value is not a valid integer. Path: /PathOrder/Lines/PathLine[0]/Qty | LineNumber: 1 | BytePositionInLine: 79.");
        }

        [TestMethod]
        public async Task Read_OfValuesFromAStream_StartsThePathAtTheValue()
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<Lines><PathLine><Qty>1</Qty></PathLine><PathLine><Qty>x</Qty></PathLine></Lines>"));
            var read = new List<int>();
            XmlException? thrown = null;
            try
            {
                await foreach (var line in XmlSerializer.DeserializeAsyncEnumerable<PathLine>(stream, Bare))
                {
                    read.Add(line!.Qty);
                }
            }
            catch (XmlException ex)
            {
                thrown = ex;
            }

            read.Should().Equal(1);
            thrown.Should().NotBeNull();
            (thrown!.Path + "|" + thrown.Message).Should().Be("/PathLine/Qty|The value is not a valid integer. Path: /PathLine/Qty | LineNumber: 1 | BytePositionInLine: 63.");
        }

        [TestMethod]
        public void Write_SetsThePathToTheElementsThatAreOpen()
        {
            // The writer refuses a character XML cannot carry. A path has no positions when an object is written,
            // as in System.Text.Json, and there is no line to give.
            var element = new PathOrder { Lines = [new PathLine { Label = "ok" }, new PathLine { Label = "a\0b" }] };
            Describe(() => XmlSerializer.Serialize(element, Bare)).Should().Be("/PathOrder/Lines/PathLine|:|Illegal character in XML value. Path: /PathOrder/Lines/PathLine.");

            var attribute = new PathOrder { Lines = [new PathLine { Tag = "a\0b" }] };
            Describe(() => XmlSerializer.Serialize(attribute, Bare)).Should().Be("/PathOrder/Lines/PathLine|:|Illegal character in XML value. Path: /PathOrder/Lines/PathLine.");

            var deep = new XmlSerializerOptions { MaxDepth = 2, OmitXmlDeclaration = true };
            Describe(() => XmlSerializer.Serialize(new PathOrder { Lines = [new PathLine()] }, deep)).Should().Be("/PathOrder/Lines|:|MaxDepth exceeded (2). Path: /PathOrder/Lines.");

            // Nine elements deep, which is deeper than the writer keeps in place.
            var node = new PathNode { Label = "a\0b" };
            for (var depth = 0; depth < 8; depth++)
            {
                node = new PathNode { Next = node };
            }

            Describe(() => XmlSerializer.Serialize(node, Bare)).Should().Be("/PathNode/Next/Next/Next/Next/Next/Next/Next/Next|:|Illegal character in XML value. Path: /PathNode/Next/Next/Next/Next/Next/Next/Next/Next.");

            Describe(() => XmlSerializer.Serialize(new PathOrder { Odd = new PathOdd() }, Bare)).Should().Be("/PathOrder/Odd|:|mine");
            Describe(() => XmlSerializer.Serialize(new PathOrder { Odd = new PathOdd { GivenPath = "/given" } }, Bare)).Should().Be("/given|:|mine");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Runs <paramref name="action"/> and describes the <see cref="XmlException"/> it throws.
        /// </summary>
        /// <param name="action">What is expected to throw.</param>
        /// <returns>The path, the line and the position in the line, and the message, with a bar between them.</returns>
        private static string Describe(Action action)
        {
            try
            {
                action();
            }
            catch (XmlException ex)
            {
                return ex.Path + "|" + ex.LineNumber + ":" + ex.LinePosition + "|" + ex.Message;
            }

            return "nothing was thrown";
        }

        #endregion

    }

}
