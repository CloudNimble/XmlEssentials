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
            RoundTrip(true, "<boolean>true</boolean>");
            RoundTrip(false, "<boolean>false</boolean>");
            RoundTrip((byte)255, "<unsignedByte>255</unsignedByte>");
            RoundTrip((sbyte)-128, "<byte>-128</byte>");
            RoundTrip((short)-32768, "<short>-32768</short>");
            RoundTrip((ushort)65535, "<unsignedShort>65535</unsignedShort>");
            RoundTrip(-2147483648, "<int>-2147483648</int>");
            RoundTrip(4294967295u, "<unsignedInt>4294967295</unsignedInt>");
            RoundTrip(-9223372036854775808L, "<long>-9223372036854775808</long>");
            RoundTrip(18446744073709551615UL, "<unsignedLong>18446744073709551615</unsignedLong>");
            RoundTrip(1.5f, "<float>1.5</float>");
            RoundTrip(-0.25, "<double>-0.25</double>");
            RoundTrip(79228162514264337593543950335m, "<decimal>79228162514264337593543950335</decimal>");
            RoundTrip("a <b> & \"c\"", "<string>a &lt;b> &amp; \"c\"</string>");
            RoundTrip('é', "<char>é</char>");
            RoundTrip(Guid.Parse("00112233-4455-6677-8899-aabbccddeeff"), "<guid>00112233-4455-6677-8899-aabbccddeeff</guid>");
            RoundTrip(new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc), "<dateTime>2020-01-02T03:04:05Z</dateTime>");
            RoundTrip(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(-7)), "<dateTimeOffset>2020-01-02T03:04:05-07:00</dateTimeOffset>");
            RoundTrip(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero), "<dateTimeOffset>2020-01-02T03:04:05+00:00</dateTimeOffset>");
            RoundTrip(new DateOnly(2020, 1, 2), "<dateOnly>2020-01-02</dateOnly>");
            RoundTrip(new TimeOnly(3, 4, 5), "<timeOnly>03:04:05</timeOnly>");
            RoundTrip(TimeSpan.FromHours(1), "<TimeSpan>PT1H</TimeSpan>");
            RoundTrip(new Uri("https://example.com/a?b=1&c=2"), "<Uri>https://example.com/a?b=1&amp;c=2</Uri>");
            RoundTrip(new Uri("relative/path", UriKind.Relative), "<Uri>relative/path</Uri>");
            RoundTrip(new Version(1, 2), "<Version>1.2</Version>");
            RoundTrip(new Version(2147483647, 2147483647, 2147483647, 2147483647), "<Version>2147483647.2147483647.2147483647.2147483647</Version>");
            XmlSerializer.Serialize(new byte[] { 1, 2, 3 }, Bare).Should().Be("<base64Binary>AQID</base64Binary>");
            XmlSerializer.Deserialize<byte[]>("<base64Binary>AQID</base64Binary>").Should().Equal(1, 2, 3);
        }

        [TestMethod]
        public void Declaration_IsWrittenUnlessOmitted_AndWebDefaultsUseCamelCase()
        {
            XmlSerializer.Serialize(42).Should().Be(Declaration + "<int>42</int>");
            XmlSerializer.Serialize(42, XmlSerializerOptions.Web).Should().Be("<int>42</int>");
            XmlSerializer.Deserialize<int>(Declaration + "<int>42</int>").Should().Be(42);
            XmlSerializer.Deserialize<int>("<int>42</int>", XmlSerializerOptions.Web).Should().Be(42);
        }

        [TestMethod]
        public void DefaultNamespace_IsDeclaredOnTheRoot_AndRequiredWhenReading()
        {
            var namespaced = new XmlSerializerOptions { DefaultNamespace = "urn:x", OmitXmlDeclaration = true };
            XmlSerializer.Serialize(42, namespaced).Should().Be("<int xmlns=\"urn:x\">42</int>");
            XmlSerializer.Deserialize<int>("<int xmlns=\"urn:x\">42</int>", namespaced).Should().Be(42);
            XmlSerializer.Deserialize<int>("<p:int xmlns:p=\"urn:x\">42</p:int>", namespaced).Should().Be(42);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>("<int>42</int>", namespaced))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element is not in namespace 'urn:x'. Path: /int | LineNumber: 1 | BytePositionInLine: 6.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize("<int>42</int>", typeof(int), namespaced))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element is not in namespace 'urn:x'. Path: /int | LineNumber: 1 | BytePositionInLine: 6.");
        }

        [TestMethod]
        public void RootName_MustMatchTheType()
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>("<Nope>42</Nope>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'int'. Path: /int | LineNumber: 1 | BytePositionInLine: 7.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize("<Nope>42</Nope>", typeof(int)))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The root element does not match 'int'. Path: /int | LineNumber: 1 | BytePositionInLine: 7.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>(""))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Document does not have a root element. Path: /int | LineNumber: 1 | BytePositionInLine: 1.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>("<!--only a comment-->"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Document does not have a root element. Path: /int | LineNumber: 1 | BytePositionInLine: 22.");
        }

        [TestMethod]
        public void LexicalForms_FollowXsd_SurroundingWhitespaceIsCollapsed()
        {
            XmlSerializer.Deserialize<int>("<int>+007</int>").Should().Be(7);
            XmlSerializer.Deserialize<int>("<int>\r\n  42\t</int>").Should().Be(42);
            XmlSerializer.Deserialize<bool>("<boolean>1</boolean>").Should().BeTrue();
            XmlSerializer.Deserialize<bool>("<boolean> 0 </boolean>").Should().BeFalse();
            XmlSerializer.Deserialize<char>("<char> Z </char>").Should().Be('Z');
            XmlSerializer.Deserialize<Version>("<Version> 1.2.3 </Version>").Should().Be(new Version(1, 2, 3));
            XmlSerializer.Deserialize<Uri>("<Uri> https://example.com/ </Uri>").Should().Be(new Uri("https://example.com/"));
            XmlSerializer.Deserialize<string>("<string> kept as written </string>").Should().Be(" kept as written ");
        }

        [TestMethod]
        public void InvalidLexicalForm_ThrowsXmlException_NamingTheKindOfValue()
        {
            Rejects<bool>("<boolean>yes</boolean>", "The value is not a boolean.");
            Rejects<byte>("<unsignedByte>256</unsignedByte>", "The value is not a valid integer.");
            Rejects<sbyte>("<byte>128</byte>", "The value is not a valid integer.");
            Rejects<short>("<short>32768</short>", "The value is not a valid integer.");
            Rejects<ushort>("<unsignedShort>-1</unsignedShort>", "The value is not a valid integer.");
            Rejects<int>("<int>1.0</int>", "The value is not a valid integer.");
            Rejects<uint>("<unsignedInt>-1</unsignedInt>", "The value is not a valid integer.");
            Rejects<long>("<long>9223372036854775808</long>", "The value is not a valid integer.");
            Rejects<ulong>("<unsignedLong>-1</unsignedLong>", "The value is not a valid integer.");
            Rejects<float>("<float>nope</float>", "The value is not a valid float.");
            Rejects<double>("<double>nope</double>", "The value is not a valid double.");
            Rejects<decimal>("<decimal>1e2</decimal>", "The value is not a valid decimal.");
            Rejects<char>("<char>ab</char>", "The value is not a single character.");
            Rejects<char>("<char>𝄞</char>", "The value is not a single character.");
            Rejects<Guid>("<guid>nope</guid>", "The value is not a valid GUID.");
            Rejects<DateTime>("<dateTime>March 5, 2020</dateTime>", "The value is not a valid dateTime.");
            Rejects<DateTimeOffset>("<dateTimeOffset>nope</dateTimeOffset>", "The value is not a valid dateTime.");
            Rejects<DateOnly>("<dateOnly>2020-13-01</dateOnly>", "The value is not a valid date.");
            Rejects<TimeOnly>("<timeOnly>25:00:00</timeOnly>", "The value is not a valid time.");
            Rejects<TimeSpan>("<TimeSpan>PT1D</TimeSpan>", "The value is not a valid duration.");
            Rejects<Uri>("<Uri>http://exa mple</Uri>", "The value is not a valid URI.");
            Rejects<Version>("<Version>1</Version>", "The value is not a valid version.");
            Rejects<byte[]>("<base64Binary>!!!!</base64Binary>", "The value is not valid base64.");
        }

        [TestMethod]
        public void EmptyElement_IsAnEmptyStringUriOrByteArray_AndInvalidForEveryOtherType()
        {
            XmlSerializer.Serialize("", Bare).Should().Be("<string></string>");
            XmlSerializer.Deserialize<string>("<string/>").Should().Be("");
            XmlSerializer.Deserialize<string>("<string></string>").Should().Be("");
            XmlSerializer.Deserialize<byte[]>("<base64Binary/>").Should().BeEmpty();
            XmlSerializer.Deserialize<Uri>("<Uri/>").Should().Be(new Uri("", UriKind.Relative));
            Rejects<int>("<int/>", "The value is not a valid integer.");
            Rejects<int>("<int></int>", "The value is not a valid integer.");
            Rejects<int?>("<int/>", "The value is not a valid integer.");
            Rejects<bool>("<boolean/>", "The value is not a boolean.");
            Rejects<char>("<char/>", "The value is not a single character.");
            Rejects<Guid>("<guid/>", "The value is not a valid GUID.");
            Rejects<Version>("<Version/>", "The value is not a valid version.");
        }

        [TestMethod]
        public void Content_SplitByCommentsInstructionsCDataOrReferences_IsReadAsOneValue()
        {
            XmlSerializer.Deserialize<int>("<int><!--c-->42<?pi d?></int>").Should().Be(42);
            XmlSerializer.Deserialize<int>("<int>4<!--c-->2</int>").Should().Be(42);
            XmlSerializer.Deserialize<int>("<int><![CDATA[4]]>2</int>").Should().Be(42);
            XmlSerializer.Deserialize<int>("<int>&#52;2</int>").Should().Be(42);
            XmlSerializer.Deserialize<int>("<int>&#52;<!--c-->&#50;</int>").Should().Be(42);
            XmlSerializer.Deserialize<string>("<string>a<![CDATA[<b>]]>c&amp;d<!--e-->f</string>").Should().Be("a<b>c&df");
            XmlSerializer.Deserialize<string>("<string><![CDATA[]]>tail</string>").Should().Be("tail");

            var part = new string('x', 300);
            var split = "<string>" + part + "<!--c-->" + part + "<![CDATA[" + part + "]]>" + part + "<?pi d?>" + string.Concat(System.Linq.Enumerable.Repeat(part + "<!--c-->", 20)) + "</string>";
            XmlSerializer.Deserialize<string>(split).Should().Be(string.Concat(System.Linq.Enumerable.Repeat(part, 24)));
        }

        [TestMethod]
        public void ChildElement_InsideAScalar_IsRejected()
        {
            Rejects<int>("<int><x/></int>", "Unexpected StartElement in simple content.", position: 10);
            Rejects<string>("<string>a<b/>c</string>", "Unexpected StartElement in simple content.", position: 14);
        }

        [TestMethod]
        public void Null_IsWrittenAsXsiNil_AndReadBack()
        {
            XmlSerializer.Serialize<string?>(null, Bare).Should().Be("<string" + Nil + "/>");
            XmlSerializer.Serialize((int?)null, Bare).Should().Be("<int" + Nil + "/>");
            XmlSerializer.Serialize((object?)null, typeof(string), Bare).Should().Be("<string" + Nil + "/>");
            XmlSerializer.Serialize((object?)null, typeof(int?), Bare).Should().Be("<int" + Nil + "/>");
            XmlSerializer.Serialize((int?)8, Bare).Should().Be("<int>8</int>");
            XmlSerializer.Serialize((object)8, typeof(int?), Bare).Should().Be("<int>8</int>");

            XmlSerializer.Deserialize<string>("<string" + Nil + "/>").Should().BeNull();
            XmlSerializer.Deserialize<string>("<string" + Nil + ">ignored</string>").Should().BeNull();
            XmlSerializer.Deserialize("<string" + Nil + ">ignored</string>", typeof(string)).Should().BeNull();
            XmlSerializer.Deserialize<int?>("<int" + Nil + "/>").Should().BeNull();
            XmlSerializer.Deserialize<int?>("<int" + Nil + ">9</int>").Should().BeNull();
            XmlSerializer.Deserialize<int?>("<int>8</int>").Should().Be(8);
            XmlSerializer.Deserialize<int?>("<int xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"false\">8</int>").Should().Be(8);
            XmlSerializer.Deserialize<string>("<string other=\"1\">a</string>").Should().Be("a");
        }

        [TestMethod]
        public void NullRoot_IsWrittenAsNil_UnderEveryIgnoreCondition()
        {
            foreach (var condition in new[] { XmlIgnoreCondition.Never, XmlIgnoreCondition.WhenWritingNull, XmlIgnoreCondition.WhenWritingDefault })
            {
                var options = new XmlSerializerOptions { DefaultIgnoreCondition = condition, OmitXmlDeclaration = true };
                XmlSerializer.Serialize<string?>(null, options).Should().Be("<string" + Nil + "/>");
                XmlSerializer.Serialize((int?)null, options).Should().Be("<int" + Nil + "/>");
            }
        }

        [TestMethod]
        public void Null_ForATypeThatCannotBeNull_IsRejectedBothWays()
        {
            FluentActions.Invoking(() => XmlSerializer.Serialize((object?)null, typeof(int)))
                .Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be("Type 'System.Int32' cannot be null.");
            Rejects<int>("<int" + Nil + "/>", "The element is nil but type 'System.Int32' is not nillable.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize("<int" + Nil + "/>", typeof(int)))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The element is nil but type 'System.Int32' is not nillable. Path: /int | LineNumber: 1 | BytePositionInLine: 76.");
        }

        [TestMethod]
        public void HandleNullConverter_IsGivenTheNilElement()
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            options.Converters.Add(new SentinelIntConverter());
            XmlSerializer.Deserialize<int>("<int" + Nil + "/>", options).Should().Be(-1);
            XmlSerializer.Deserialize<int>("<int" + Nil + ">9</int>", options).Should().Be(-1);
            XmlSerializer.Deserialize("<int" + Nil + "/>", typeof(int), options).Should().Be(-1);
            XmlSerializer.Deserialize<int>("<int>7</int>", options).Should().Be(7);
        }

        [TestMethod]
        public void NamedFloatingPointLiterals_AreWrittenAndRead_UnlessNumberHandlingIsStrict()
        {
            XmlSerializer.Serialize(double.NaN, Bare).Should().Be("<double>NaN</double>");
            XmlSerializer.Serialize(double.PositiveInfinity, Bare).Should().Be("<double>INF</double>");
            XmlSerializer.Serialize(double.NegativeInfinity, Bare).Should().Be("<double>-INF</double>");
            XmlSerializer.Serialize(float.NaN, Bare).Should().Be("<float>NaN</float>");
            XmlSerializer.Serialize(float.PositiveInfinity, Bare).Should().Be("<float>INF</float>");
            XmlSerializer.Deserialize<double>("<double>NaN</double>").Should().Be(double.NaN);
            XmlSerializer.Deserialize<double>("<double>-INF</double>").Should().Be(double.NegativeInfinity);
            XmlSerializer.Deserialize<float>("<float>INF</float>").Should().Be(float.PositiveInfinity);

            var strict = new XmlSerializerOptions { NumberHandling = XmlNumberHandling.Strict, OmitXmlDeclaration = true };
            XmlSerializer.Serialize(1.5, strict).Should().Be("<double>1.5</double>");
            XmlSerializer.Deserialize<double>("<double>1.5</double>", strict).Should().Be(1.5);
            XmlSerializer.Deserialize<float>("<float>1.5</float>", strict).Should().Be(1.5f);
            const string message = "Named floating-point literals are not allowed when NumberHandling is Strict.";
            FluentActions.Invoking(() => XmlSerializer.Serialize(double.NaN, strict)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message + " Path: /double.");
            FluentActions.Invoking(() => XmlSerializer.Serialize(float.NegativeInfinity, strict)).Should().ThrowExactly<XmlException>().Which.Message.Should().Be(message + " Path: /float.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<double>("<double>INF</double>", strict))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid double. Path: /double | LineNumber: 1 | BytePositionInLine: 21.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<float>("<float>NaN</float>", strict))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid float. Path: /float | LineNumber: 1 | BytePositionInLine: 19.");
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
                var reader = new Utf8XmlReader("<int>4"u8, isFinalBlock: false, new XmlReaderState());
                XmlSerializer.Deserialize<int>(ref reader);
            }).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The element is incomplete: more data is required. Path: /int | LineNumber: 1 | BytePositionInLine: 6.");
            FluentActions.Invoking(() =>
            {
                var reader = new Utf8XmlReader("<?xml version=\"1.0\"?>"u8, isFinalBlock: false, new XmlReaderState());
                XmlSerializer.Deserialize<int>(ref reader);
            }).Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Expected a start element. Path: /int | LineNumber: 1 | BytePositionInLine: 22.");
        }

        [TestMethod]
        public void ReaderOverload_ReadsOneElement_AndLeavesTheReaderOnItsEnd()
        {
            var reader = new Utf8XmlReader("<r><int>1</int><int>2</int></r>"u8);
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
            XmlSerializer.Deserialize<int>("<int>1</int><!--after--> ").Should().Be(1);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>("<int>1</int><int>2</int>"))
                .Should().ThrowExactly<XmlException>();
            FluentActions.Invoking(() => XmlSerializer.Deserialize<int>("<int>1</int>junk"))
                .Should().ThrowExactly<XmlException>();
            FluentActions.Invoking(() => XmlSerializer.Deserialize("<int>1</int></int>", typeof(int)))
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
