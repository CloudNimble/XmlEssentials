using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// How <see cref="XmlSerializer"/> writes and reads enums of every underlying type, with and without
    /// <see cref="FlagsAttribute"/>.
    /// </summary>
    [TestClass]
    public class XmlSerializerEnumTests
    {

        #region Fields

        private static readonly XmlSerializerOptions Integer = new() { EnumHandling = XmlEnumHandling.Integer, OmitXmlDeclaration = true };
        private static readonly XmlSerializerOptions Name = new() { OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void Member_IsWrittenAsItsName_OrItsNumberUnderIntegerHandling()
        {
            XmlSerializer.Serialize(DayOfWeek.Monday, Name).Should().Be("<DayOfWeek>Monday</DayOfWeek>");
            XmlSerializer.Serialize(DayOfWeek.Monday, Integer).Should().Be("<DayOfWeek>1</DayOfWeek>");
            XmlSerializer.Serialize(SmallState.Max, Name).Should().Be("<SmallState>Max</SmallState>");
            XmlSerializer.Serialize(SmallState.Max, Integer).Should().Be("<SmallState>255</SmallState>");
            XmlSerializer.Serialize(SignedLevel.Low, Name).Should().Be("<SignedLevel>Low</SignedLevel>");
            XmlSerializer.Serialize(SignedLevel.Low, Integer).Should().Be("<SignedLevel>-1</SignedLevel>");
            XmlSerializer.Serialize(ShortKind.Below, Integer).Should().Be("<ShortKind>-300</ShortKind>");
            XmlSerializer.Serialize(ShortKind.Above, Name).Should().Be("<ShortKind>Above</ShortKind>");
            XmlSerializer.Serialize(LongCode.Smallest, Integer).Should().Be("<LongCode>-9223372036854775808</LongCode>");
            XmlSerializer.Serialize(LongCode.Big, Name).Should().Be("<LongCode>Big</LongCode>");
            XmlSerializer.Serialize(WideFlags.High, Integer).Should().Be("<WideFlags>9223372036854775808</WideFlags>");
            XmlSerializer.Serialize(WideFlags.High, Name).Should().Be("<WideFlags>High</WideFlags>");
        }

        [TestMethod]
        public void Member_IsReadFromItsNameOrItsNumber_WhateverTheHandling()
        {
            XmlSerializer.Deserialize<DayOfWeek>("<DayOfWeek>Monday</DayOfWeek>", Integer).Should().Be(DayOfWeek.Monday);
            XmlSerializer.Deserialize<DayOfWeek>("<DayOfWeek> 1 </DayOfWeek>", Name).Should().Be(DayOfWeek.Monday);
            XmlSerializer.Deserialize<SmallState>("<SmallState>255</SmallState>", Name).Should().Be(SmallState.Max);
            XmlSerializer.Deserialize<SmallState>("<SmallState>Off</SmallState>", Name).Should().Be(SmallState.Off);
            XmlSerializer.Deserialize<SignedLevel>("<SignedLevel>-1</SignedLevel>", Name).Should().Be(SignedLevel.Low);
            XmlSerializer.Deserialize<ShortKind>("<ShortKind>-300</ShortKind>", Name).Should().Be(ShortKind.Below);
            XmlSerializer.Deserialize<ShortKind>("<ShortKind>Above</ShortKind>", Name).Should().Be(ShortKind.Above);
            XmlSerializer.Deserialize<LongCode>("<LongCode>-9223372036854775808</LongCode>", Name).Should().Be(LongCode.Smallest);
            XmlSerializer.Deserialize<LongCode>("<LongCode>5000000000</LongCode>", Name).Should().Be(LongCode.Big);
            XmlSerializer.Deserialize<WideFlags>("<WideFlags>9223372036854775808</WideFlags>", Name).Should().Be(WideFlags.High);
            XmlSerializer.Deserialize<DayOfWeek?>("<DayOfWeek>Tuesday</DayOfWeek>", Name).Should().Be(DayOfWeek.Tuesday);
        }

        [TestMethod]
        public void NullableEnum_UsesXsiNilForNull()
        {
            XmlSerializer.Serialize((DayOfWeek?)DayOfWeek.Tuesday, Name).Should().Be("<DayOfWeek>Tuesday</DayOfWeek>");
            XmlSerializer.Serialize((DayOfWeek?)null, Name).Should().Be(
                "<DayOfWeek xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>");
            XmlSerializer.Deserialize<DayOfWeek?>(
                "<DayOfWeek xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>", Name).Should().BeNull();
        }

        [TestMethod]
        public void Flags_AreWrittenAsSpaceSeparatedNames_InValueOrder()
        {
            XmlSerializer.Serialize(AttributeTargets.Class | AttributeTargets.Method, Name)
                .Should().Be("<AttributeTargets>Class Method</AttributeTargets>");
            XmlSerializer.Serialize(AttributeTargets.Class | AttributeTargets.Method, Integer)
                .Should().Be("<AttributeTargets>68</AttributeTargets>");
            XmlSerializer.Serialize(AttributeTargets.All, Name).Should().Be("<AttributeTargets>All</AttributeTargets>");
            XmlSerializer.Serialize(WideFlags.Low | WideFlags.High, Name).Should().Be("<WideFlags>Low High</WideFlags>");
            XmlSerializer.Serialize(WideFlags.Low | WideFlags.Mid, Name).Should().Be("<WideFlags>LowMid</WideFlags>");
            XmlSerializer.Serialize(WideFlags.LowMid | WideFlags.High, Name).Should().Be("<WideFlags>LowMid High</WideFlags>");
            XmlSerializer.Serialize(WideFlags.Mid | WideFlags.High, Name).Should().Be("<WideFlags>Mid High</WideFlags>");
        }

        [TestMethod]
        public void Value_WithNoName_IsWrittenAsItsNumber()
        {
            XmlSerializer.Serialize((SmallState)7, Name).Should().Be("<SmallState>7</SmallState>");
            XmlSerializer.Serialize((SignedLevel)(-5), Name).Should().Be("<SignedLevel>-5</SignedLevel>");
            XmlSerializer.Serialize((WideFlags)0, Name).Should().Be("<WideFlags>0</WideFlags>");
            XmlSerializer.Serialize((WideFlags)4, Name).Should().Be("<WideFlags>4</WideFlags>");
            XmlSerializer.Serialize(WideFlags.Low | (WideFlags)4, Name).Should().Be("<WideFlags>5</WideFlags>");
            XmlSerializer.Serialize((WideFlags)ulong.MaxValue, Name).Should().Be("<WideFlags>18446744073709551615</WideFlags>");
        }

        [TestMethod]
        public void Flags_AreReadFromNamesOrAnyNumberThatFits()
        {
            XmlSerializer.Deserialize<AttributeTargets>("<AttributeTargets>Class Method</AttributeTargets>", Name)
                .Should().Be(AttributeTargets.Class | AttributeTargets.Method);
            XmlSerializer.Deserialize<AttributeTargets>("<AttributeTargets>\n  Method\n  Class\n</AttributeTargets>", Name)
                .Should().Be(AttributeTargets.Class | AttributeTargets.Method);
            XmlSerializer.Deserialize<AttributeTargets>("<AttributeTargets>68</AttributeTargets>", Integer)
                .Should().Be(AttributeTargets.Class | AttributeTargets.Method);
            XmlSerializer.Deserialize<WideFlags>("<WideFlags>Low High</WideFlags>", Name).Should().Be(WideFlags.Low | WideFlags.High);
            XmlSerializer.Deserialize<WideFlags>("<WideFlags>0</WideFlags>", Name).Should().Be((WideFlags)0);
            XmlSerializer.Deserialize<WideFlags>("<WideFlags>5</WideFlags>", Name).Should().Be(WideFlags.Low | (WideFlags)4);
            XmlSerializer.Deserialize<WideFlags>("<WideFlags>18446744073709551615</WideFlags>", Name).Should().Be((WideFlags)ulong.MaxValue);
        }

        [TestMethod]
        public void UnknownNames_UndeclaredNumbers_AndNumbersOutOfRange_AreRejected()
        {
            Rejects<DayOfWeek>("<DayOfWeek>Nope</DayOfWeek>");
            Rejects<DayOfWeek>("<DayOfWeek>monday</DayOfWeek>");
            Rejects<DayOfWeek>("<DayOfWeek> </DayOfWeek>");
            Rejects<DayOfWeek>("<DayOfWeek/>");
            Rejects<DayOfWeek>("<DayOfWeek>Monday Tuesday</DayOfWeek>");
            Rejects<DayOfWeek>("<DayOfWeek>7</DayOfWeek>");
            Rejects<DayOfWeek>("<DayOfWeek>1x</DayOfWeek>");
            Rejects<SmallState>("<SmallState>2</SmallState>");
            Rejects<SmallState>("<SmallState>256</SmallState>");
            Rejects<SmallState>("<SmallState>-1</SmallState>");
            Rejects<SignedLevel>("<SignedLevel>128</SignedLevel>");
            Rejects<SignedLevel>("<SignedLevel>-129</SignedLevel>");
            Rejects<ShortKind>("<ShortKind>65236</ShortKind>");
            Rejects<LongCode>("<LongCode>9223372036854775808</LongCode>");
            Rejects<AttributeTargets>("<AttributeTargets>4294967296</AttributeTargets>");
            Rejects<AttributeTargets>("<AttributeTargets>Class Nope</AttributeTargets>");
            Rejects<WideFlags>("<WideFlags>-1</WideFlags>");
            Rejects<WideFlags>("<WideFlags>18446744073709551616</WideFlags>");
        }

        #endregion

        #region Private Methods

        private static void Rejects<T>(string xml)
        {
            FluentActions.Invoking(() => XmlSerializer.Deserialize<T>(xml, Name))
                .Should().ThrowExactly<XmlException>().Which.Message.Should().Be("The value is not a valid enum member.");
        }

        #endregion

    }

}
