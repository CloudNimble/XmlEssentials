using System;
using System.Collections.Generic;
using System.Numerics;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="Int128"/>, <see cref="UInt128"/>, <see cref="Half"/>, <see cref="BigInteger"/>,
    /// <see cref="Memory{T}"/> and <see cref="ReadOnlyMemory{T}"/> of bytes, and a <see cref="char"/> that is
    /// white space: each is one value in the document, and reads back as the value that was written.
    /// </summary>
    [TestClass]
    public class XmlSerializerWideScalarTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Bare = new() { NewLine = "\n", OmitXmlDeclaration = true };
        private const string Nil = "xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"";

        #endregion

        #region Public Methods

        [TestMethod]
        public void BigInteger_Root_IsWrittenAsItsDigits()
        {
            var value = BigInteger.Parse("-123456789012345678901234567890123456789012345678901234567890");
            var xml = XmlSerializer.Serialize(value, Bare);
            xml.Should().Be("<BigInteger>-123456789012345678901234567890123456789012345678901234567890</BigInteger>");
            XmlSerializer.Deserialize<BigInteger>(xml).Should().Be(value);
            XmlSerializer.Serialize(BigInteger.Zero, Bare).Should().Be("<BigInteger>0</BigInteger>");
            XmlSerializer.Deserialize<BigInteger>("<BigInteger> +0042 </BigInteger>").Should().Be(new BigInteger(42));
            FluentActions.Invoking(() => XmlSerializer.Deserialize<BigInteger>("<BigInteger>4.2</BigInteger>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid integer. Path: /BigInteger | LineNumber: 1 | BytePositionInLine: 29.");
        }

        [TestMethod]
        [DataRow(' ', "<char> </char>", DisplayName = "a blank")]
        [DataRow('\t', "<char>\t</char>", DisplayName = "a tab")]
        [DataRow('\n', "<char>\n</char>", DisplayName = "a line feed")]
        [DataRow('\r', "<char>&#xD;</char>", DisplayName = "a carriage return, written as a reference so it is not normalized")]
        [DataRow('x', "<char>x</char>", DisplayName = "a letter")]
        public void Char_ThatIsWhiteSpace_RoundTrips(char value, string expected)
        {
            var xml = XmlSerializer.Serialize(value, Bare);
            xml.Should().Be(expected);
            XmlSerializer.Deserialize<char>(xml).Should().Be(value);
        }

        [TestMethod]
        [DataRow(' ', "<CharAttribute Mark=\" \"/>", DisplayName = "a blank")]
        [DataRow('\t', "<CharAttribute Mark=\"&#x9;\"/>", DisplayName = "a tab, written as a reference so it is not normalized to a blank")]
        [DataRow('\n', "<CharAttribute Mark=\"&#xA;\"/>", DisplayName = "a line feed, written as a reference")]
        [DataRow('\r', "<CharAttribute Mark=\"&#xD;\"/>", DisplayName = "a carriage return, written as a reference")]
        [DataRow('x', "<CharAttribute Mark=\"x\"/>", DisplayName = "a letter")]
        public void Char_ThatIsWhiteSpace_RoundTripsAsAnAttribute(char value, string expected)
        {
            var xml = XmlSerializer.Serialize(new CharAttribute { Mark = value }, Bare);
            xml.Should().Be(expected);
            XmlSerializer.Deserialize<CharAttribute>(xml)!.Mark.Should().Be(value);
        }

        [TestMethod]
        public void Char_WithWhiteSpaceAroundIt_IsTheCharacter()
        {
            XmlSerializer.Deserialize<char>("<char> x </char>").Should().Be('x');
            XmlSerializer.Deserialize<char>("<char>\n  x\n</char>").Should().Be('x');
            FluentActions.Invoking(() => XmlSerializer.Deserialize<char>("<char>  </char>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a single character. Path: /char | LineNumber: 1 | BytePositionInLine: 16.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<char>("<char>xy</char>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a single character. Path: /char | LineNumber: 1 | BytePositionInLine: 16.");
        }

        [TestMethod]
        public void Half_Root_IsWrittenAsTheShortestNumberThatReadsBack()
        {
            XmlSerializer.Serialize((Half)1.5, Bare).Should().Be("<Half>1.5</Half>");
            XmlSerializer.Serialize((Half)0.1, Bare).Should().Be("<Half>0.1</Half>");
            XmlSerializer.Serialize(Half.NaN, Bare).Should().Be("<Half>NaN</Half>");
            XmlSerializer.Serialize(Half.PositiveInfinity, Bare).Should().Be("<Half>INF</Half>");
            XmlSerializer.Serialize(Half.NegativeInfinity, Bare).Should().Be("<Half>-INF</Half>");
            XmlSerializer.Deserialize<Half>("<Half>1.5</Half>").Should().Be((Half)1.5);
            XmlSerializer.Deserialize<Half>("<Half>0.1</Half>").Should().Be((Half)0.1);
            XmlSerializer.Deserialize<Half>("<Half>-INF</Half>").Should().Be(Half.NegativeInfinity);
            Half.IsNaN(XmlSerializer.Deserialize<Half>("<Half>NaN</Half>")).Should().BeTrue();
            XmlSerializer.Deserialize<Half>("<Half>1e9</Half>").Should().Be(Half.PositiveInfinity, "a number too large for the type reads as infinity, as it does for a float");
        }

        [TestMethod]
        public void Half_ThatIsNotFinite_IsRejectedWhenNumberHandlingIsStrict()
        {
            var strict = new XmlSerializerOptions { OmitXmlDeclaration = true, NumberHandling = Text.Xml.Serialization.XmlNumberHandling.Strict };
            FluentActions.Invoking(() => XmlSerializer.Serialize(Half.NaN, strict))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("Named floating-point literals are not allowed when NumberHandling is Strict. Path: /Half.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Half>("<Half>INF</Half>", strict))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid float. Path: /Half | LineNumber: 1 | BytePositionInLine: 17.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Half>("<Half>one</Half>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid float. Path: /Half | LineNumber: 1 | BytePositionInLine: 17.");
            XmlSerializer.Serialize((Half)2, strict).Should().Be("<Half>2</Half>");
        }

        [TestMethod]
        public void Int128_Root_IsWrittenAsItsDigits()
        {
            XmlSerializer.Serialize(Int128.MaxValue, Bare).Should().Be("<Int128>170141183460469231731687303715884105727</Int128>");
            XmlSerializer.Serialize(Int128.MinValue, Bare).Should().Be("<Int128>-170141183460469231731687303715884105728</Int128>");
            XmlSerializer.Deserialize<Int128>("<Int128>170141183460469231731687303715884105727</Int128>").Should().Be(Int128.MaxValue);
            XmlSerializer.Deserialize<Int128>("<Int128>-170141183460469231731687303715884105728</Int128>").Should().Be(Int128.MinValue);
            XmlSerializer.Deserialize<Int128>("<Int128> +5 </Int128>").Should().Be((Int128)5);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Int128>("<Int128>170141183460469231731687303715884105728</Int128>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid integer. Path: /Int128 | LineNumber: 1 | BytePositionInLine: 57.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Int128>("<Int128/>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid integer. Path: /Int128 | LineNumber: 1 | BytePositionInLine: 10.");
        }

        [TestMethod]
        public void Memory_Root_IsWrittenAsBase64()
        {
            var bytes = new byte[] { 1, 2, 3, 250 };
            XmlSerializer.Serialize(new Memory<byte>(bytes), Bare).Should().Be("<MemoryOfByte>AQID+g==</MemoryOfByte>");
            XmlSerializer.Serialize(new ReadOnlyMemory<byte>(bytes), Bare).Should().Be("<ReadOnlyMemoryOfByte>AQID+g==</ReadOnlyMemoryOfByte>");
            XmlSerializer.Deserialize<Memory<byte>>("<MemoryOfByte>AQID+g==</MemoryOfByte>").ToArray().Should().Equal(bytes);
            XmlSerializer.Deserialize<ReadOnlyMemory<byte>>("<ReadOnlyMemoryOfByte>AQID+g==</ReadOnlyMemoryOfByte>").ToArray().Should().Equal(bytes);
            XmlSerializer.Serialize(Memory<byte>.Empty, Bare).Should().Be("<MemoryOfByte></MemoryOfByte>");
            XmlSerializer.Deserialize<Memory<byte>>("<MemoryOfByte/>").Length.Should().Be(0);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<Memory<byte>>("<MemoryOfByte>!</MemoryOfByte>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not valid base64. Path: /MemoryOfByte | LineNumber: 1 | BytePositionInLine: 31.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<ReadOnlyMemory<byte>>("<ReadOnlyMemoryOfByte>!</ReadOnlyMemoryOfByte>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not valid base64. Path: /ReadOnlyMemoryOfByte | LineNumber: 1 | BytePositionInLine: 47.");
        }

        [TestMethod]
        public void UInt128_Root_IsWrittenAsItsDigits()
        {
            XmlSerializer.Serialize(UInt128.MaxValue, Bare).Should().Be("<UInt128>340282366920938463463374607431768211455</UInt128>");
            XmlSerializer.Deserialize<UInt128>("<UInt128>340282366920938463463374607431768211455</UInt128>").Should().Be(UInt128.MaxValue);
            XmlSerializer.Deserialize<UInt128>("<UInt128>-0</UInt128>").Should().Be(UInt128.Zero);
            FluentActions.Invoking(() => XmlSerializer.Deserialize<UInt128>("<UInt128>-1</UInt128>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid integer. Path: /UInt128 | LineNumber: 1 | BytePositionInLine: 22.");
            FluentActions.Invoking(() => XmlSerializer.Deserialize<UInt128>("<UInt128>340282366920938463463374607431768211456</UInt128>"))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid integer. Path: /UInt128 | LineNumber: 1 | BytePositionInLine: 59.");
        }

        [TestMethod]
        public void WideScalars_AsMembers_RoundTrip()
        {
            var value = new WideScalars
            {
                Code = (Int128)7,
                Mark = ' ',
                Big = BigInteger.Pow(10, 30),
                Bytes = new byte[] { 1, 2 },
                Letter = ' ',
                Ratio = (Half)2.5,
                Maybe = null,
                Sealed = new byte[] { 3 },
                Totals = [UInt128.One, UInt128.MaxValue],
            };
            var xml = XmlSerializer.Serialize(value, Bare);
            xml.Should().Be(
                "<WideScalars Code=\"7\" Mark=\" \"><Big>1000000000000000000000000000000</Big><Bytes>AQI=</Bytes><Letter> </Letter><Ratio>2.5</Ratio>" +
                "<Maybe " + Nil + "/><Sealed>Aw==</Sealed><Totals>1</Totals><Totals>340282366920938463463374607431768211455</Totals></WideScalars>");
            var read = XmlSerializer.Deserialize<WideScalars>(xml)!;
            read.Code.Should().Be((Int128)7);
            read.Mark.Should().Be(' ');
            read.Big.Should().Be(BigInteger.Pow(10, 30));
            read.Bytes.ToArray().Should().Equal(1, 2);
            read.Letter.Should().Be(' ');
            read.Ratio.Should().Be((Half)2.5);
            read.Maybe.Should().BeNull();
            read.Sealed.ToArray().Should().Equal(3);
            read.Totals.Should().Equal(UInt128.One, UInt128.MaxValue);
            XmlSerializer.Deserialize<WideScalars>("<WideScalars><Maybe>-9</Maybe></WideScalars>")!.Maybe.Should().Be((Int128)(-9));
        }

        [TestMethod]
        public void WideScalars_AsDictionaryValues_RoundTrip()
        {
            var xml = XmlSerializer.Serialize(new Dictionary<string, Int128> { ["a"] = 5, ["b"] = -6 }, Bare);
            xml.Should().Be("<DictionaryOfStringInt128><a>5</a><b>-6</b></DictionaryOfStringInt128>");
            XmlSerializer.Deserialize<Dictionary<string, Int128>>(xml).Should().Equal(new Dictionary<string, Int128> { ["a"] = 5, ["b"] = -6 });
        }

        #endregion

    }

}
