using System;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// What <see cref="XmlSerializer"/> writes and reads for each built-in scalar type, stated as exact documents.
    /// </summary>
    [TestClass]
    public class XmlSerializerScalarTests
    {

        #region Fields

        private const string Declaration = "<?xml version=\"1.0\" encoding=\"utf-8\"?>";
        private const string Nil = " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";
        private static readonly XmlSerializerOptions Bare = new() { OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void BuiltInTypes_WriteOneElementNamedAfterTheType_AndReadItBack()
        {
            RoundTrip(true, "<Boolean>true</Boolean>");
            RoundTrip(false, "<Boolean>false</Boolean>");
            RoundTrip((byte)255, "<Byte>255</Byte>");
            RoundTrip((sbyte)-128, "<SByte>-128</SByte>");
            RoundTrip((short)-32768, "<Int16>-32768</Int16>");
            RoundTrip((ushort)65535, "<UInt16>65535</UInt16>");
            RoundTrip(-2147483648, "<Int32>-2147483648</Int32>");
            RoundTrip(4294967295u, "<UInt32>4294967295</UInt32>");
            RoundTrip(-9223372036854775808L, "<Int64>-9223372036854775808</Int64>");
            RoundTrip(18446744073709551615UL, "<UInt64>18446744073709551615</UInt64>");
            RoundTrip(1.5f, "<Single>1.5</Single>");
            RoundTrip(-0.25, "<Double>-0.25</Double>");
            RoundTrip(79228162514264337593543950335m, "<Decimal>79228162514264337593543950335</Decimal>");
            RoundTrip("a <b> & \"c\"", "<String>a &lt;b> &amp; \"c\"</String>");
            RoundTrip('é', "<Char>é</Char>");
            RoundTrip(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"), "<Guid>00112233-4455-6677-8899-aabbccddeeff</Guid>");
            RoundTrip(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc), "<DateTime>2020-01-02T03:04:05Z</DateTime>");
            RoundTrip(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(-7)), "<DateTimeOffset>2020-01-02T03:04:05-07:00</DateTimeOffset>");
            RoundTrip(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero), "<DateTimeOffset>2020-01-02T03:04:05+00:00</DateTimeOffset>");
            RoundTrip(new DateOnly(2020, 1, 2), "<DateOnly>2020-01-02</DateOnly>");
            RoundTrip(new TimeOnly(3, 4, 5), "<TimeOnly>03:04:05</TimeOnly>");
            RoundTrip(TimeSpan.FromHours(1), "<TimeSpan>PT1H</TimeSpan>");
            RoundTrip(new Uri("https://example.com/a?b=1&c=2"), "<Uri>https://example.com/a?b=1&amp;c=2</Uri>");
            RoundTrip(new Uri("relative/path", UriKind.Relative), "<Uri>relative/path</Uri>");
            RoundTrip(new Version(1, 2), "<Version>1.2</Version>");
            RoundTrip(new Version(2147483647, 2147483647, 2147483647, 2147483647), "<Version>2147483647.2147483647.2147483647.2147483647</Version>");
            XmlSerializer.Serialize(new byte[] { 1, 2, 3 }, Bare).Should().Be("<ByteArray>AQID</ByteArray>");
            XmlSerializer.Deserialize<byte[]>("<ByteArray>AQID</ByteArray>").Should().Equal(1, 2, 3);
        }

        [TestMethod]
        public void Declaration_IsWrittenUnlessOmitted_AndWebDefaultsUseCamelCase()
        {
            XmlSerializer.Serialize(42).Should().Be(Declaration + "<Int32>42</Int32>");
            XmlSerializer.Serialize(42, XmlSerializerOptions.Web).Should().Be("<int32>42</int32>");
            XmlSerializer.Deserialize<int>(Declaration + "<Int32>42</Int32>").Should().Be(42);
            XmlSerializer.Deserialize<int>("<int32>42</int32>", XmlSerializerOptions.Web).Should().Be(42);
        }

        [TestMethod]
        public void DefaultNamespace_IsDeclaredOnTheRoot_AndRequiredWhenReading()
        {
            var namespaced = new XmlSerializerOptions { DefaultNamespace = "urn:x", OmitXmlDeclaration = true };
            XmlSerializer.Serialize(42, namespaced).Should().Be("<Int32 xmlns=\"urn:x\">42</Int32>");
            XmlSerializer.Deserialize<int>("<Int32 xmlns=\"urn:x\">42</Int32>", namespaced).Should().Be(42);
            XmlSerializer.Deserialize<int>("<p:Int32 xmlns:p=\"urn:x\">42</p:Int32>", namespaced).Should().Be(42);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>("<Int32>42</Int32>", namespaced))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element is not in namespace 'urn:x'. Path: /Int32 | LineNumber: 1 | BytePositionInLine: 8.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize("<Int32>42</Int32>", typeof(int), namespaced))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element is not in namespace 'urn:x'. Path: /Int32 | LineNumber: 1 | BytePositionInLine: 8.");
        }

        [TestMethod]
        public void RootName_MustMatchTheType()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>("<Nope>42</Nope>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'Int32'. Path: /Int32 | LineNumber: 1 | BytePositionInLine: 7.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize("<Nope>42</Nope>", typeof(int)))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'Int32'. Path: /Int32 | LineNumber: 1 | BytePositionInLine: 7.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>(""))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Document does not have a root element. Path: /Int32 | LineNumber: 1 | BytePositionInLine: 1.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>("<!--only a comment-->"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Document does not have a root element. Path: /Int32 | LineNumber: 1 | BytePositionInLine: 22.");
        }

        [TestMethod]
        public void LexicalForms_FollowXsd_SurroundingWhitespaceIsCollapsed()
        {
            XmlSerializer.Deserialize<int>("<Int32>+007</Int32>").Should().Be(7);
            XmlSerializer.Deserialize<int>("<Int32>\r\n  42\t</Int32>").Should().Be(42);
            XmlSerializer.Deserialize<bool>("<Boolean>1</Boolean>").Should().BeTrue();
            XmlSerializer.Deserialize<bool>("<Boolean> 0 </Boolean>").Should().BeFalse();
            XmlSerializer.Deserialize<char>("<Char> Z </Char>").Should().Be('Z');
            XmlSerializer.Deserialize<Version>("<Version> 1.2.3 </Version>").Should().Be(new Version(1, 2, 3));
            XmlSerializer.Deserialize<Uri>("<Uri> https://example.com/ </Uri>").Should().Be(new Uri("https://example.com/"));
            XmlSerializer.Deserialize<string>("<String> kept as written </String>").Should().Be(" kept as written ");
        }

        [TestMethod]
        public void InvalidLexicalForm_ThrowsXmlException_NamingTheKindOfValue()
        {
            Rejects<bool>("<Boolean>yes</Boolean>", "The value is not a boolean.");
            Rejects<byte>("<Byte>256</Byte>", "The value is not a valid integer.");
            Rejects<sbyte>("<SByte>128</SByte>", "The value is not a valid integer.");
            Rejects<short>("<Int16>32768</Int16>", "The value is not a valid integer.");
            Rejects<ushort>("<UInt16>-1</UInt16>", "The value is not a valid integer.");
            Rejects<int>("<Int32>1.0</Int32>", "The value is not a valid integer.");
            Rejects<uint>("<UInt32>-1</UInt32>", "The value is not a valid integer.");
            Rejects<long>("<Int64>9223372036854775808</Int64>", "The value is not a valid integer.");
            Rejects<ulong>("<UInt64>-1</UInt64>", "The value is not a valid integer.");
            Rejects<float>("<Single>nope</Single>", "The value is not a valid float.");
            Rejects<double>("<Double>nope</Double>", "The value is not a valid double.");
            Rejects<decimal>("<Decimal>1e2</Decimal>", "The value is not a valid decimal.");
            Rejects<char>("<Char>ab</Char>", "The value is not a single character.");
            Rejects<char>("<Char>𝄞</Char>", "The value is not a single character.");
            Rejects<Guid>("<Guid>nope</Guid>", "The value is not a valid GUID.");
            Rejects<DateTime>("<DateTime>March 5, 2020</DateTime>", "The value is not a valid dateTime.");
            Rejects<DateTimeOffset>("<DateTimeOffset>nope</DateTimeOffset>", "The value is not a valid dateTime.");
            Rejects<DateOnly>("<DateOnly>2020-13-01</DateOnly>", "The value is not a valid date.");
            Rejects<TimeOnly>("<TimeOnly>25:00:00</TimeOnly>", "The value is not a valid time.");
            Rejects<TimeSpan>("<TimeSpan>PT1D</TimeSpan>", "The value is not a valid duration.");
            Rejects<Uri>("<Uri>http://exa mple</Uri>", "The value is not a valid URI.");
            Rejects<Version>("<Version>1</Version>", "The value is not a valid version.");
            Rejects<byte[]>("<ByteArray>!!!!</ByteArray>", "The value is not valid base64.");
        }

        [TestMethod]
        public void EmptyElement_IsAnEmptyStringUriOrByteArray_AndInvalidForEveryOtherType()
        {
            XmlSerializer.Serialize("", Bare).Should().Be("<String></String>");
            XmlSerializer.Deserialize<string>("<String/>").Should().Be("");
            XmlSerializer.Deserialize<string>("<String></String>").Should().Be("");
            XmlSerializer.Deserialize<byte[]>("<ByteArray/>").Should().BeEmpty();
            XmlSerializer.Deserialize<Uri>("<Uri/>").Should().Be(new Uri("", UriKind.Relative));
            Rejects<int>("<Int32/>", "The value is not a valid integer.");
            Rejects<int>("<Int32></Int32>", "The value is not a valid integer.");
            Rejects<int?>("<Int32/>", "The value is not a valid integer.");
            Rejects<bool>("<Boolean/>", "The value is not a boolean.");
            Rejects<char>("<Char/>", "The value is not a single character.");
            Rejects<Guid>("<Guid/>", "The value is not a valid GUID.");
            Rejects<Version>("<Version/>", "The value is not a valid version.");
        }

        [TestMethod]
        public void Content_SplitByCommentsInstructionsCDataOrReferences_IsReadAsOneValue()
        {
            XmlSerializer.Deserialize<int>("<Int32><!--c-->42<?pi d?></Int32>").Should().Be(42);
            XmlSerializer.Deserialize<int>("<Int32>4<!--c-->2</Int32>").Should().Be(42);
            XmlSerializer.Deserialize<int>("<Int32><![CDATA[4]]>2</Int32>").Should().Be(42);
            XmlSerializer.Deserialize<int>("<Int32>&#52;2</Int32>").Should().Be(42);
            XmlSerializer.Deserialize<int>("<Int32>&#52;<!--c-->&#50;</Int32>").Should().Be(42);
            XmlSerializer.Deserialize<string>("<String>a<![CDATA[<b>]]>c&amp;d<!--e-->f</String>").Should().Be("a<b>c&df");
            XmlSerializer.Deserialize<string>("<String><![CDATA[]]>tail</String>").Should().Be("tail");

            var part = new string('x', 300);
            var split = "<String>" + part + "<!--c-->" + part + "<![CDATA[" + part + "]]>" + part + "<?pi d?>" + string.Concat(System.Linq.Enumerable.Repeat(part + "<!--c-->", 20)) + "</String>";
            XmlSerializer.Deserialize<string>(split).Should().Be(string.Concat(System.Linq.Enumerable.Repeat(part, 24)));
        }

        [TestMethod]
        public void ChildElement_InsideAScalar_IsRejected()
        {
            Rejects<int>("<Int32><x/></Int32>", "Unexpected StartElement in simple content.", position: 12);
            Rejects<string>("<String>a<b/>c</String>", "Unexpected StartElement in simple content.", position: 14);
        }

        [TestMethod]
        public void Null_IsWrittenAsXsiNil_AndReadBack()
        {
            XmlSerializer.Serialize<string?>(null, Bare).Should().Be("<String" + Nil + "/>");
            XmlSerializer.Serialize((int?)null, Bare).Should().Be("<Int32" + Nil + "/>");
            XmlSerializer.Serialize((object?)null, typeof(string), Bare).Should().Be("<String" + Nil + "/>");
            XmlSerializer.Serialize((object?)null, typeof(int?), Bare).Should().Be("<Int32" + Nil + "/>");
            XmlSerializer.Serialize((int?)8, Bare).Should().Be("<Int32>8</Int32>");
            XmlSerializer.Serialize((object)8, typeof(int?), Bare).Should().Be("<Int32>8</Int32>");

            XmlSerializer.Deserialize<string>("<String" + Nil + "/>").Should().BeNull();
            XmlSerializer.Deserialize<string>("<String" + Nil + ">ignored</String>").Should().BeNull();
            XmlSerializer.Deserialize("<String" + Nil + ">ignored</String>", typeof(string)).Should().BeNull();
            XmlSerializer.Deserialize<int?>("<Int32" + Nil + "/>").Should().BeNull();
            XmlSerializer.Deserialize<int?>("<Int32" + Nil + ">9</Int32>").Should().BeNull();
            XmlSerializer.Deserialize<int?>("<Int32>8</Int32>").Should().Be(8);
            XmlSerializer.Deserialize<int?>("<Int32 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"false\">8</Int32>").Should().Be(8);
            XmlSerializer.Deserialize<string>("<String other=\"1\">a</String>").Should().Be("a");
        }

        [TestMethod]
        public void NullRoot_IsWrittenAsNil_UnderEveryIgnoreCondition()
        {
            foreach (var condition in new[] { XmlIgnoreCondition.Never, XmlIgnoreCondition.WhenWritingNull, XmlIgnoreCondition.WhenWritingDefault })
            {
                var options = new XmlSerializerOptions { DefaultIgnoreCondition = condition, OmitXmlDeclaration = true };
                XmlSerializer.Serialize<string?>(null, options).Should().Be("<String" + Nil + "/>");
                XmlSerializer.Serialize((int?)null, options).Should().Be("<Int32" + Nil + "/>");
            }
        }

        [TestMethod]
        public void Null_ForATypeThatCannotBeNull_IsRejectedBothWays()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize((object?)null, typeof(int)))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("Type 'System.Int32' cannot be null.");
            Rejects<int>("<Int32" + Nil + "/>", "The element is nil but type 'System.Int32' is not nillable.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize("<Int32" + Nil + "/>", typeof(int)))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The element is nil but type 'System.Int32' is not nillable. Path: /Int32 | LineNumber: 1 | BytePositionInLine: 78.");
        }

        [TestMethod]
        public void HandleNullConverter_IsGivenTheNilElement()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            options.Converters.Add(new SentinelIntConverter());
            XmlSerializer.Deserialize<int>("<Int32" + Nil + "/>", options).Should().Be(-1);
            XmlSerializer.Deserialize<int>("<Int32" + Nil + ">9</Int32>", options).Should().Be(-1);
            XmlSerializer.Deserialize("<Int32" + Nil + "/>", typeof(int), options).Should().Be(-1);
            XmlSerializer.Deserialize<int>("<Int32>7</Int32>", options).Should().Be(7);
        }

        [TestMethod]
        public void NamedFloatingPointLiterals_AreWrittenAndRead_UnlessNumberHandlingIsStrict()
        {
            XmlSerializer.Serialize(double.NaN, Bare).Should().Be("<Double>NaN</Double>");
            XmlSerializer.Serialize(double.PositiveInfinity, Bare).Should().Be("<Double>INF</Double>");
            XmlSerializer.Serialize(double.NegativeInfinity, Bare).Should().Be("<Double>-INF</Double>");
            XmlSerializer.Serialize(float.NaN, Bare).Should().Be("<Single>NaN</Single>");
            XmlSerializer.Serialize(float.PositiveInfinity, Bare).Should().Be("<Single>INF</Single>");
            XmlSerializer.Deserialize<double>("<Double>NaN</Double>").Should().Be(double.NaN);
            XmlSerializer.Deserialize<double>("<Double>-INF</Double>").Should().Be(double.NegativeInfinity);
            XmlSerializer.Deserialize<float>("<Single>INF</Single>").Should().Be(float.PositiveInfinity);

            var strict = new XmlSerializerOptions { NumberHandling = XmlNumberHandling.Strict, OmitXmlDeclaration = true };
            XmlSerializer.Serialize(1.5, strict).Should().Be("<Double>1.5</Double>");
            XmlSerializer.Deserialize<double>("<Double>1.5</Double>", strict).Should().Be(1.5);
            XmlSerializer.Deserialize<float>("<Single>1.5</Single>", strict).Should().Be(1.5f);
            const string message = "Named floating-point literals are not allowed when NumberHandling is Strict.";
            FluentActions.Invoking(() => XmlSerializer.Serialize(double.NaN, strict)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message + " Path: /Double.");
            FluentActions.Invoking(() => XmlSerializer.Serialize(float.NegativeInfinity, strict)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message + " Path: /Single.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<double>("<Double>INF</Double>", strict))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid double. Path: /Double | LineNumber: 1 | BytePositionInLine: 21.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<float>("<Single>NaN</Single>", strict))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid float. Path: /Single | LineNumber: 1 | BytePositionInLine: 21.");
        }

        [TestMethod]
        public void Converter_ReadsAndWritesAnAttribute_WithOrWithoutAPrefix()
        {
            var options = XmlSerializerOptions.Default;
            var converter = options.GetTypeInfo<int>().Converter;
            var reader = new Utf8XmlReader("<e n=\" 42 \"/>"u8);
            reader.Read();
            reader.MoveToNextAttribute();
            converter.ReadAsAttribute(ref reader, typeof(int), options).Should().Be(42);

            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("e");
                converter.WriteAsAttribute(writer, new XmlName("n"), 42, options);
                converter.WriteAsAttribute(writer, new XmlName("m", "urn:x", "p"), 7, options);
                writer.WriteEndElement();
            }).Should().Be("<e n=\"42\" xmlns:p=\"urn:x\" p:m=\"7\"/>");

            var nullable = options.GetTypeInfo<int?>().Converter;
            reader = new Utf8XmlReader("<e n=\"5\"/>"u8);
            reader.Read();
            reader.MoveToNextAttribute();
            nullable.ReadAsAttribute(ref reader, typeof(int?), options).Should().Be(5);
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("e");
                nullable.WriteAsAttribute(writer, new XmlName("n"), 6, options);
                writer.WriteEndElement();
            }).Should().Be("<e n=\"6\"/>");
        }

        [TestMethod]
        public void Converter_WithoutAttributeSupport_SaysSo()
        {
            var converter = new DoublingNumberConverter();
            var options = new XmlSerializerOptions();
            var message = $"Converter '{typeof(DoublingNumberConverter).FullName}' does not support attributes.";
            FluentActions.Invoking(() =>
            {
                var reader = new Utf8XmlReader("<e n=\"1\"/>"u8);
                reader.Read();
                reader.MoveToNextAttribute();
                converter.ReadAsAttribute(ref reader, typeof(DoublingNumber), options);
            }).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(message);
            XmlWriterHarness.ToXml(writer =>
            {
                writer.WriteStartElement("e");
                FluentActions.Invoking(() => converter.WriteAsAttribute(writer, new XmlName("n"), new DoublingNumber { Value = 1 }, options))
                    .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(message);
                writer.WriteEndElement();
            }).Should().Be("<e/>");
        }

        [TestMethod]
        public void IncompleteInput_FromAReaderThatIsNotOnItsFinalBlock_Throws()
        {
            FluentActions.Invoking(() =>
            {
                var reader = new Utf8XmlReader("<Int32>4"u8, isFinalBlock: false, new XmlReaderState());
                XmlSerializer.Deserialize<int>(ref reader);
            }).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The element is incomplete: more data is required. Path: /Int32 | LineNumber: 1 | BytePositionInLine: 8.");
            FluentActions.Invoking(() =>
            {
                var reader = new Utf8XmlReader("<?xml version=\"1.0\"?>"u8, isFinalBlock: false, new XmlReaderState());
                XmlSerializer.Deserialize<int>(ref reader);
            }).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Expected a start element. Path: /Int32 | LineNumber: 1 | BytePositionInLine: 22.");
        }

        [TestMethod]
        public void ReaderOverload_ReadsOneElement_AndLeavesTheReaderOnItsEnd()
        {
            var reader = new Utf8XmlReader("<r><Int32>1</Int32><Int32>2</Int32></r>"u8);
            reader.Read();
            reader.Read();
            XmlSerializer.Deserialize<int>(ref reader).Should().Be(1);
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            XmlSerializer.Deserialize<int>(ref reader).Should().Be(2);
            reader.Read().Should().BeTrue();
            reader.TokenType.Should().Be(XmlTokenType.EndElement);
            Encoding.UTF8.GetString(reader.LocalNameSpan).Should().Be("r");
        }

        [TestMethod]
        public void WholeDocumentOverloads_RejectAnythingAfterTheRootThatIsNotWellFormed()
        {
            XmlSerializer.Deserialize<int>("<Int32>1</Int32><!--after--> ").Should().Be(1);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>("<Int32>1</Int32><Int32>2</Int32>"))
                .Should().ThrowExactly<XmlException>();
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>("<Int32>1</Int32>junk"))
                .Should().ThrowExactly<XmlException>();
            FluentActions.Invoking(() => XmlSerializer.Deserialize("<Int32>1</Int32></Int32>", typeof(int)))
                .Should().ThrowExactly<XmlException>();
        }

        #endregion

        #region Private Methods

        private static void Rejects<T>(string xml, string message, int position = 0)
        {
            // The path is the document element, whose name the document gives. The reader has read all of it
            // unless the test says where it stopped.
            var name = xml[1..xml.IndexOfAny(['>', '/', ' '])];
            var stopped = position == 0 ? System.Text.Encoding.UTF8.GetByteCount(xml) + 1 : position;
            FluentActions.Invoking(() => XmlSerializer.Deserialize<T>(xml))
                .Should().ThrowExactly<XmlException>().Which.Message
                .Should().Be(message + " Path: /" + name + " | LineNumber: 1 | BytePositionInLine: " + stopped + ".");
        }

        private static void RoundTrip<T>(T value, string xml)
        {
            XmlSerializer.Serialize(value, Bare).Should().Be(xml);
            XmlSerializer.Deserialize<T>(xml).Should().Be(value);
            XmlSerializer.Serialize(value, typeof(T), Bare).Should().Be(xml);
            XmlSerializer.Deserialize(xml, typeof(T)).Should().Be(value);
        }

        #endregion

    }

}
