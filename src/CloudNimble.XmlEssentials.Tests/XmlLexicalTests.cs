using System;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// XML Schema 1.0 lexical forms, checked through <see cref="XmlLexical"/>, <see cref="Utf8XmlReader"/>,
    /// and <see cref="XmlElement"/> so the three can never disagree.
    /// </summary>
    [TestClass]
    public class XmlLexicalTests
    {

        #region Public Methods

        [TestMethod]
        [DataRow("true", true)]
        [DataRow("1", true)]
        [DataRow("false", false)]
        [DataRow("0", false)]
        [DataRow(" \t\r\ntrue\n ", true)]
        public void Boolean_Valid(string text, bool expected)
        {
            XmlLexical.TryParseBoolean(Utf8(text), out var value).Should().BeTrue();
            value.Should().Be(expected);
            ReaderText(text).GetBoolean().Should().Be(expected);
            using var document = Document(text);
            document.RootElement.GetBoolean().Should().Be(expected);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("TRUE")]
        [DataRow("True")]
        [DataRow("yes")]
        [DataRow("2")]
        [DataRow("tr ue")]
        [DataRow("01")]
        public void Boolean_Invalid(string text)
        {
            XmlLexical.TryParseBoolean(Utf8(text), out _).Should().BeFalse();
            DomThrows(text, e => e.GetBoolean());
        }

        [TestMethod]
        [DataRow("0", 0L)]
        [DataRow("-0", 0L)]
        [DataRow("+0", 0L)]
        [DataRow("42", 42L)]
        [DataRow(" 42 ", 42L)]
        [DataRow("\n\t42\r\n", 42L)]
        [DataRow("+5", 5L)]
        [DataRow("-5", -5L)]
        [DataRow("007", 7L)]
        [DataRow("9223372036854775807", long.MaxValue)]
        [DataRow("-9223372036854775808", long.MinValue)]
        [DataRow("0000000000000000000000000000009223372036854775807", long.MaxValue)]
        [DataRow("00000000000000000000000", 0L)]
        public void Int64_Valid(string text, long expected)
        {
            XmlLexical.TryParseInt64(Utf8(text), out var value).Should().BeTrue();
            value.Should().Be(expected);
            ReaderText(text).GetInt64().Should().Be(expected);
            using var document = Document(text);
            document.RootElement.GetInt64().Should().Be(expected);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("+")]
        [DataRow("-")]
        [DataRow("+-1")]
        [DataRow("--1")]
        [DataRow("1 2")]
        [DataRow("1.0")]
        [DataRow("1e3")]
        [DataRow("1,000")]
        [DataRow("0x10")]
        [DataRow("12a")]
        [DataRow("١")]
        [DataRow("9223372036854775808")]
        [DataRow("-9223372036854775809")]
        [DataRow("18446744073709551616")]
        [DataRow("99999999999999999999")]
        [DataRow("100000000000000000000")]
        public void Int64_Invalid(string text)
        {
            XmlLexical.TryParseInt64(Utf8(text), out _).Should().BeFalse();
            ReaderText(text).TryGetInt64(out _).Should().BeFalse();
            DomThrows(text, e => e.GetInt64());
        }

        [TestMethod]
        public void Integers_RangeBoundaries()
        {
            XmlLexical.TryParseInt32(Utf8("2147483647"), out var maxInt).Should().BeTrue();
            maxInt.Should().Be(int.MaxValue);
            XmlLexical.TryParseInt32(Utf8("-2147483648"), out var minInt).Should().BeTrue();
            minInt.Should().Be(int.MinValue);
            XmlLexical.TryParseInt32(Utf8("2147483648"), out _).Should().BeFalse();
            XmlLexical.TryParseInt32(Utf8("-2147483649"), out _).Should().BeFalse();

            XmlLexical.TryParseInt16(Utf8("32767"), out var maxShort).Should().BeTrue();
            maxShort.Should().Be(short.MaxValue);
            XmlLexical.TryParseInt16(Utf8("-32768"), out var minShort).Should().BeTrue();
            minShort.Should().Be(short.MinValue);
            XmlLexical.TryParseInt16(Utf8("32768"), out _).Should().BeFalse();
            XmlLexical.TryParseInt16(Utf8("-32769"), out _).Should().BeFalse();

            XmlLexical.TryParseSByte(Utf8("127"), out var maxSByte).Should().BeTrue();
            maxSByte.Should().Be(sbyte.MaxValue);
            XmlLexical.TryParseSByte(Utf8("-128"), out var minSByte).Should().BeTrue();
            minSByte.Should().Be(sbyte.MinValue);
            XmlLexical.TryParseSByte(Utf8("128"), out _).Should().BeFalse();
            XmlLexical.TryParseSByte(Utf8("-129"), out _).Should().BeFalse();

            XmlLexical.TryParseUInt64(Utf8("18446744073709551615"), out var maxULong).Should().BeTrue();
            maxULong.Should().Be(ulong.MaxValue);
            XmlLexical.TryParseUInt64(Utf8("+7"), out var plusULong).Should().BeTrue();
            plusULong.Should().Be(7UL);
            XmlLexical.TryParseUInt64(Utf8("-0"), out var negativeZero).Should().BeTrue();
            negativeZero.Should().Be(0UL);
            XmlLexical.TryParseUInt64(Utf8("-1"), out _).Should().BeFalse();
            XmlLexical.TryParseUInt64(Utf8("18446744073709551616"), out _).Should().BeFalse();

            XmlLexical.TryParseUInt32(Utf8("4294967295"), out var maxUInt).Should().BeTrue();
            maxUInt.Should().Be(uint.MaxValue);
            XmlLexical.TryParseUInt32(Utf8("4294967296"), out _).Should().BeFalse();
            XmlLexical.TryParseUInt16(Utf8("65535"), out var maxUShort).Should().BeTrue();
            maxUShort.Should().Be(ushort.MaxValue);
            XmlLexical.TryParseUInt16(Utf8("65536"), out _).Should().BeFalse();
            XmlLexical.TryParseByte(Utf8("255"), out var maxByte).Should().BeTrue();
            maxByte.Should().Be(byte.MaxValue);
            XmlLexical.TryParseByte(Utf8("256"), out _).Should().BeFalse();
            XmlLexical.TryParseByte(Utf8("x"), out _).Should().BeFalse();
        }

        [TestMethod]
        public void Reader_IntegerGetters_UseTheSharedRules()
        {
            ReaderText(" +12 ").GetInt32().Should().Be(12);
            ReaderText(" +12 ").GetInt16().Should().Be(12);
            ReaderText(" +12 ").GetSByte().Should().Be(12);
            ReaderText(" +12 ").GetByte().Should().Be(12);
            ReaderText(" +12 ").GetUInt16().Should().Be(12);
            ReaderText(" +12 ").GetUInt32().Should().Be(12U);
            ReaderText(" +12 ").GetUInt64().Should().Be(12UL);
            ReaderText("1e3").TryGetInt32(out _).Should().BeFalse();
            ReaderText("300").TryGetByte(out _).Should().BeFalse();
            using var document = XmlDocument.Parse("<e a=\" +12 \"> -3 </e>");
            document.RootElement.GetAttribute("a").GetInt32().Should().Be(12);
            document.RootElement.GetInt32().Should().Be(-3);
            DomThrows("2147483648", e => e.GetInt32());
        }

        [TestMethod]
        [DataRow("0", "0")]
        [DataRow("1.5", "1.5")]
        [DataRow("-1.5", "-1.5")]
        [DataRow("+1.5", "1.5")]
        [DataRow(" 12.50 ", "12.50")]
        [DataRow(".5", "0.5")]
        [DataRow("5.", "5")]
        [DataRow("-.25", "-0.25")]
        [DataRow("79228162514264337593543950335", "79228162514264337593543950335")]
        public void Decimal_Valid(string text, string expected)
        {
            var expectedValue = decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture);
            XmlLexical.TryParseDecimal(Utf8(text), out var value).Should().BeTrue();
            value.Should().Be(expectedValue);
            ReaderText(text).GetDecimal().Should().Be(expectedValue);
            using var document = Document(text);
            document.RootElement.GetDecimal().Should().Be(expectedValue);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(".")]
        [DataRow("+")]
        [DataRow("-")]
        [DataRow("+-1")]
        [DataRow("1e2")]
        [DataRow("1E+2")]
        [DataRow("1,000")]
        [DataRow("1.2.3")]
        [DataRow("1 000")]
        [DataRow("INF")]
        [DataRow("NaN")]
        [DataRow("$1")]
        [DataRow("79228162514264337593543950336")]
        public void Decimal_Invalid(string text)
        {
            XmlLexical.TryParseDecimal(Utf8(text), out _).Should().BeFalse();
            ReaderText(text).TryGetDecimal(out _).Should().BeFalse();
            DomThrows(text, e => e.GetDecimal());
        }

        [TestMethod]
        [DataRow("0", 0d)]
        [DataRow("-0", -0d)]
        [DataRow("3.5", 3.5d)]
        [DataRow("+3.5", 3.5d)]
        [DataRow(" 3.5 ", 3.5d)]
        [DataRow(".5", 0.5d)]
        [DataRow("5.", 5d)]
        [DataRow("1e3", 1000d)]
        [DataRow("1E3", 1000d)]
        [DataRow("1.5e-3", 0.0015d)]
        [DataRow("-1.5E+3", -1500d)]
        [DataRow("INF", double.PositiveInfinity)]
        [DataRow("-INF", double.NegativeInfinity)]
        [DataRow("NaN", double.NaN)]
        [DataRow("inf", double.PositiveInfinity)]
        [DataRow("-Inf", double.NegativeInfinity)]
        [DataRow("nan", double.NaN)]
        [DataRow("1.7976931348623157E308", double.MaxValue)]
        public void Double_Valid(string text, double expected)
        {
            XmlLexical.TryParseDouble(Utf8(text), out var value).Should().BeTrue();
            value.Should().Be(expected);
            ReaderText(text).GetDouble().Should().Be(expected);
            using var document = Document(text);
            document.RootElement.GetDouble().Should().Be(expected);
            XmlLexical.TryParseSingle(Utf8(text), out var single).Should().BeTrue();
            single.Should().Be((float)expected);
            ReaderText(text).GetSingle().Should().Be((float)expected);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(".")]
        [DataRow("e3")]
        [DataRow("1e")]
        [DataRow("1e+")]
        [DataRow("1e1.5")]
        [DataRow("+-1")]
        [DataRow("+INF")]
        [DataRow("Infinity")]
        [DataRow("-Infinity")]
        [DataRow("∞")]
        [DataRow("1,5")]
        [DataRow("0x1p3")]
        [DataRow("1 e3")]
        public void Double_Invalid(string text)
        {
            XmlLexical.TryParseDouble(Utf8(text), out _).Should().BeFalse();
            XmlLexical.TryParseSingle(Utf8(text), out _).Should().BeFalse();
            ReaderText(text).TryGetDouble(out _).Should().BeFalse();
            ReaderText(text).TryGetSingle(out _).Should().BeFalse();
            DomThrows(text, e => e.GetDouble());
        }

        [TestMethod]
        [DataRow("2020-01-02T03:04:05", 2020, 1, 2, 3, 4, 5, 0, DateTimeKind.Unspecified)]
        [DataRow(" 2020-01-02T03:04:05 ", 2020, 1, 2, 3, 4, 5, 0, DateTimeKind.Unspecified)]
        [DataRow("2020-01-02T03:04:05Z", 2020, 1, 2, 3, 4, 5, 0, DateTimeKind.Utc)]
        [DataRow("2020-01-02T03:04:05+00:00", 2020, 1, 2, 3, 4, 5, 0, DateTimeKind.Utc)]
        [DataRow("2020-01-02T03:04:05.5", 2020, 1, 2, 3, 4, 5, 5_000_000, DateTimeKind.Unspecified)]
        [DataRow("2020-01-02T03:04:05.1234567", 2020, 1, 2, 3, 4, 5, 1_234_567, DateTimeKind.Unspecified)]
        [DataRow("2020-01-02T03:04:05.12345678999", 2020, 1, 2, 3, 4, 5, 1_234_567, DateTimeKind.Unspecified)]
        [DataRow("2020-02-29T23:59:59", 2020, 2, 29, 23, 59, 59, 0, DateTimeKind.Unspecified)]
        [DataRow("2020-12-31T24:00:00", 2021, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)]
        [DataRow("0001-01-01T00:00:00", 1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)]
        [DataRow("9999-12-31T23:59:59.9999999", 9999, 12, 31, 23, 59, 59, 9_999_999, DateTimeKind.Unspecified)]
        public void DateTime_Valid(string text, int year, int month, int day, int hour, int minute, int second, int fractionTicks, DateTimeKind kind)
        {
            var expected = new DateTime(year, month, day, hour, minute, second, kind).AddTicks(fractionTicks);
            XmlLexical.TryParseDateTime(Utf8(text), out var value).Should().BeTrue();
            value.Should().Be(expected);
            value.Kind.Should().Be(kind);
            ReaderText(text).GetDateTime().Should().Be(expected);
            using var document = Document(text);
            document.RootElement.GetDateTime().Should().Be(expected);
        }

        [TestMethod]
        public void DateTime_WithOffset_IsTheSameInstant()
        {
            var expected = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.FromHours(5.5));
            XmlLexical.TryParseDateTime(Utf8("2020-01-02T03:04:05+05:30"), out var local).Should().BeTrue();
            local.Kind.Should().Be(DateTimeKind.Local);
            local.ToUniversalTime().Should().Be(expected.UtcDateTime);
            XmlLexical.TryParseDateTimeOffset(Utf8("2020-01-02T03:04:05+05:30"), out var offset).Should().BeTrue();
            offset.Should().Be(expected);
            offset.Offset.Should().Be(TimeSpan.FromHours(5.5));
            XmlLexical.TryParseDateTimeOffset(Utf8("2020-01-02T03:04:05-14:00"), out var west).Should().BeTrue();
            west.Offset.Should().Be(TimeSpan.FromHours(-14));
            XmlLexical.TryParseDateTimeOffset(Utf8("2020-01-02T03:04:05Z"), out var utc).Should().BeTrue();
            utc.Should().Be(new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero));
            XmlLexical.TryParseDateTimeOffset(Utf8("2020-01-02T03:04:05"), out var unzoned).Should().BeTrue();
            unzoned.DateTime.Should().Be(new DateTime(2020, 1, 2, 3, 4, 5));
            unzoned.Offset.Should().Be(TimeZoneInfo.Local.GetUtcOffset(new DateTime(2020, 1, 2, 3, 4, 5)));
            ReaderText("2020-01-02T03:04:05+05:30").GetDateTimeOffset().Should().Be(expected);
            using var document = Document("2020-01-02T03:04:05+05:30");
            document.RootElement.GetDateTimeOffset().Should().Be(expected);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("March 5, 2020")]
        [DataRow("2020-01-02")]
        [DataRow("2020-01-02 03:04:05")]
        [DataRow("2020-01-02t03:04:05")]
        [DataRow("2020-1-2T03:04:05")]
        [DataRow("20-01-02T03:04:05")]
        [DataRow("2020/01/02T03:04:05")]
        [DataRow("2020-01-02T3:04:05")]
        [DataRow("2020-01-02T03:04")]
        [DataRow("2020-01-02T03:04:05.")]
        [DataRow("2020-01-02T03:04:05z")]
        [DataRow("2020-01-02T03:04:05+5:30")]
        [DataRow("2020-01-02T03:04:05+0530")]
        [DataRow("2020-01-02T03:04:05+14:01")]
        [DataRow("2020-01-02T03:04:05+15:00")]
        [DataRow("2020-01-02T03:04:05+05:60")]
        [DataRow("2020-01-02T03:04:05 Z")]
        [DataRow("2020-13-01T00:00:00")]
        [DataRow("2020-00-01T00:00:00")]
        [DataRow("2020-01-32T00:00:00")]
        [DataRow("2020-01-00T00:00:00")]
        [DataRow("2021-02-29T00:00:00")]
        [DataRow("0000-01-01T00:00:00")]
        [DataRow("-2020-01-01T00:00:00")]
        [DataRow("12020-01-01T00:00:00")]
        [DataRow("2020-01-02T24:00:01")]
        [DataRow("2020-01-02T24:00:00.1")]
        [DataRow("2020-01-02T25:00:00")]
        [DataRow("2020-01-02T03:60:00")]
        [DataRow("2020-01-02T03:04:60")]
        [DataRow("9999-12-31T24:00:00")]
        [DataRow("0001-01-01T00:00:00+14:00")]
        [DataRow("9999-12-31T23:59:59-14:00")]
        public void DateTime_Invalid(string text)
        {
            XmlLexical.TryParseDateTime(Utf8(text), out _).Should().BeFalse();
            XmlLexical.TryParseDateTimeOffset(Utf8(text), out _).Should().BeFalse();
            ReaderText(text).TryGetDateTime(out _).Should().BeFalse();
            ReaderText(text).TryGetDateTimeOffset(out _).Should().BeFalse();
            DomThrows(text, e => e.GetDateTime());
            DomThrows(text, e => e.GetDateTimeOffset());
        }

        [TestMethod]
        public void DateOnly_And_TimeOnly()
        {
            XmlLexical.TryParseDateOnly(Utf8(" 2020-02-29 "), out var date).Should().BeTrue();
            date.Should().Be(new DateOnly(2020, 2, 29));
            XmlLexical.TryParseDateOnly(Utf8("2020-02-29Z"), out var zonedDate).Should().BeTrue();
            zonedDate.Should().Be(new DateOnly(2020, 2, 29));
            XmlLexical.TryParseDateOnly(Utf8("2020-02-29-05:00"), out var offsetDate).Should().BeTrue();
            offsetDate.Should().Be(new DateOnly(2020, 2, 29));
            ReaderText("2020-02-29").GetDateOnly().Should().Be(new DateOnly(2020, 2, 29));
            foreach (var invalid in new[] { "", "2020-02-30", "2020-2-29", "2020-02-29T00:00:00", "02/29/2020", "2020-02-29+5" })
            {
                XmlLexical.TryParseDateOnly(Utf8(invalid), out _).Should().BeFalse(invalid);
                ReaderText(invalid).TryGetDateOnly(out _).Should().BeFalse(invalid);
            }

            XmlLexical.TryParseTimeOnly(Utf8(" 13:20:00 "), out var time).Should().BeTrue();
            time.Should().Be(new TimeOnly(13, 20, 0));
            XmlLexical.TryParseTimeOnly(Utf8("13:20:00.25Z"), out var fractional).Should().BeTrue();
            fractional.Should().Be(new TimeOnly(13, 20, 0, 250));
            XmlLexical.TryParseTimeOnly(Utf8("24:00:00"), out var endOfDay).Should().BeTrue();
            endOfDay.Should().Be(TimeOnly.MinValue);
            XmlLexical.TryParseTimeOnly(Utf8("23:59:59+02:00"), out var zonedTime).Should().BeTrue();
            zonedTime.Should().Be(new TimeOnly(23, 59, 59));
            ReaderText("13:20:00").GetTimeOnly().Should().Be(new TimeOnly(13, 20, 0));
            foreach (var invalid in new[] { "", "13:20", "1:20:00", "13:20:00.", "24:00:01", "25:00:00", "13:60:00", "13:20:60", "13:20:00 PM", "13-20-00" })
            {
                XmlLexical.TryParseTimeOnly(Utf8(invalid), out _).Should().BeFalse(invalid);
                ReaderText(invalid).TryGetTimeOnly(out _).Should().BeFalse(invalid);
            }
        }

        [TestMethod]
        [DataRow("PT1S", 0, 0, 0, 1, 0)]
        [DataRow(" PT1S ", 0, 0, 0, 1, 0)]
        [DataRow("P1D", 1, 0, 0, 0, 0)]
        [DataRow("PT1H", 0, 1, 0, 0, 0)]
        [DataRow("PT1M", 0, 0, 1, 0, 0)]
        [DataRow("P1DT2H3M4S", 1, 2, 3, 4, 0)]
        [DataRow("P1DT4S", 1, 0, 0, 4, 0)]
        [DataRow("PT2H4S", 0, 2, 0, 4, 0)]
        [DataRow("PT1.5S", 0, 0, 0, 1, 5_000_000)]
        [DataRow("PT0.0000001S", 0, 0, 0, 0, 1)]
        [DataRow("PT90M", 0, 0, 90, 0, 0)]
        [DataRow("P0D", 0, 0, 0, 0, 0)]
        public void Duration_Valid(string text, int days, int hours, int minutes, int seconds, int fractionTicks)
        {
            var expected = new TimeSpan(days, hours, minutes, seconds) + TimeSpan.FromTicks(fractionTicks);
            XmlLexical.TryParseDuration(Utf8(text), out var value).Should().BeTrue();
            value.Should().Be(expected);
            ReaderText(text).GetTimeSpan().Should().Be(expected);
            XmlLexical.TryParseDuration(Utf8("-" + text.Trim()), out var negative).Should().BeTrue();
            negative.Should().Be(-expected);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("P")]
        [DataRow("-")]
        [DataRow("-P")]
        [DataRow("PT")]
        [DataRow("P1DT")]
        [DataRow("1D")]
        [DataRow("p1d")]
        [DataRow("P1Y")]
        [DataRow("P1M")]
        [DataRow("P0Y")]
        [DataRow("P1H")]
        [DataRow("P1S")]
        [DataRow("PT1D")]
        [DataRow("PT1Y")]
        [DataRow("PT1X")]
        [DataRow("P1D2H")]
        [DataRow("PT1M1H")]
        [DataRow("PT1S1M")]
        [DataRow("PT1H1H")]
        [DataRow("PT1.5H")]
        [DataRow("PT1.5M")]
        [DataRow("PT1.S")]
        [DataRow("PT.5S")]
        [DataRow("PT1.2.3S")]
        [DataRow("P1.5D")]
        [DataRow("PT1")]
        [DataRow("P-1D")]
        [DataRow("+P1D")]
        [DataRow("-P10675200D", DisplayName = "below the smallest TimeSpan")]
        [DataRow("-P10675199DT2H48M5.4775809S", DisplayName = "one tick below the smallest TimeSpan")]
        [DataRow("P1D T1H")]
        [DataRow("P9999999999999999D")]
        [DataRow("P999999999999999D")]
        [DataRow("PT999999999999999H")]
        public void Duration_Invalid(string text)
        {
            XmlLexical.TryParseDuration(Utf8("x" + text + "x")[1..^1], out _).Should().BeFalse();
            if (text.Trim() == text)
            {
                ReaderText(text).TryGetTimeSpan(out _).Should().BeFalse();
            }
        }

        [TestMethod]
        public void Guid_Formats()
        {
            var expected = Guid.Parse("00112233-4455-6677-8899-aabbccddeeff");
            foreach (var text in new[]
            {
                "00112233-4455-6677-8899-aabbccddeeff",
                " 00112233-4455-6677-8899-AABBCCDDEEFF ",
                "00112233445566778899aabbccddeeff",
                "{00112233-4455-6677-8899-aabbccddeeff}",
                "(00112233-4455-6677-8899-aabbccddeeff)",
            })
            {
                XmlLexical.TryParseGuid(Utf8(text), out var value).Should().BeTrue(text);
                value.Should().Be(expected);
                ReaderText(text).GetGuid().Should().Be(expected);
                using var document = Document(text);
                document.RootElement.GetGuid().Should().Be(expected);
            }

            foreach (var text in new[]
            {
                "",
                "00112233-4455-6677-8899-aabbccddeef",
                "00112233-4455-6677-8899-aabbccddeeffa",
                "0011223344556677-8899-aabbccddeeff000",
                "{00112233-4455-6677-8899-aabbccddeeff)",
                "{0x00112233,0x4455,0x6677,{0x88,0x99,0xaa,0xbb,0xcc,0xdd,0xee,0xff}}",
                "gg112233-4455-6677-8899-aabbccddeeff",
            })
            {
                XmlLexical.TryParseGuid(Utf8(text), out _).Should().BeFalse(text);
                ReaderText(text).TryGetGuid(out _).Should().BeFalse(text);
                DomThrows(text, e => e.GetGuid());
            }
        }

        [TestMethod]
        public void Base64_Forms()
        {
            XmlLexical.TryDecodeBase64(Utf8(""), out var empty).Should().BeTrue();
            empty.Should().BeEmpty();
            XmlLexical.TryDecodeBase64(Utf8("  "), out var blank).Should().BeTrue();
            blank.Should().BeEmpty();
            XmlLexical.TryDecodeBase64(Utf8("AQID"), out var exact).Should().BeTrue();
            exact.Should().Equal(1, 2, 3);
            XmlLexical.TryDecodeBase64(Utf8(" YQ== "), out var padded).Should().BeTrue();
            padded.Should().Equal((byte)'a');
            XmlLexical.TryDecodeBase64(Utf8("AQ\r\n  ID"), out var wrapped).Should().BeTrue();
            wrapped.Should().Equal(1, 2, 3);
            ReaderText("AQID").GetBytesFromBase64().Should().Equal(1, 2, 3);
            using var document = Document("AQID");
            document.RootElement.GetBytesFromBase64().Should().Equal(1, 2, 3);
            foreach (var invalid in new[] { "!!!!", "AQI", "A", "AQID=", "=AQID" })
            {
                XmlLexical.TryDecodeBase64(Utf8(invalid), out _).Should().BeFalse(invalid);
                ReaderText(invalid).TryGetBytesFromBase64(out _).Should().BeFalse(invalid);
                DomThrows(invalid, e => e.GetBytesFromBase64());
            }
        }

        [TestMethod]
        public void GetString_KeepsWhitespace_ThatTypedGettersCollapse()
        {
            ReaderText(" 42 ").GetString().Should().Be(" 42 ");
            using var document = Document(" 42 ");
            document.RootElement.GetString().Should().Be(" 42 ");
            document.RootElement.GetInt32().Should().Be(42);
            XmlLexical.Collapse(Utf8(" \t\r\n")).IsEmpty.Should().BeTrue();
            XmlLexical.Collapse(Utf8("a b")).SequenceEqual("a b"u8).Should().BeTrue();
        }

        #endregion

        #region Private Methods

        private static XmlDocument Document(string text)
        {
            return XmlDocument.Parse("<e>" + Escape(text) + "</e>", new XmlDocumentOptions { WhitespaceHandling = XmlWhitespaceHandling.Significant });
        }

        private static void DomThrows(string text, Action<XmlElement> read)
        {
            using var document = Document(text);
            var threw = false;
            try
            {
                read(document.RootElement);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue("the DOM must reject '{0}'", text);
        }

        private static string Escape(string text)
        {
            return text.Replace("&", "&amp;", StringComparison.Ordinal)
                .Replace("<", "&lt;", StringComparison.Ordinal)
                .Replace("\r", "&#13;", StringComparison.Ordinal);
        }

        private static Utf8XmlReader ReaderText(string text)
        {
            var reader = new Utf8XmlReader(Utf8("<e a=\"\">" + Escape(text) + "</e>"), new XmlReaderOptions { WhitespaceHandling = XmlWhitespaceHandling.Significant });
            reader.Read();
            if (text.Length > 0)
            {
                reader.Read();
            }
            else
            {
                reader.MoveToNextAttribute();
            }

            return reader;
        }

        private static byte[] Utf8(string text)
        {
            return Encoding.UTF8.GetBytes(text);
        }

        #endregion

    }

}
