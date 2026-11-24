using CloudNimble.XmlEssentials.Samples;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests.Samples
{

    /// <summary>
    /// Runs samples 37 to 39 and compares what each returns with the output the documentation shows.
    /// </summary>
    [TestClass]
    public class ReferenceSampleTests
    {

        #region Public Methods

        [TestMethod]
        public void Sample37_SupportedTypes()
        {
            SampleOutput.Capture(SupportedTypes.Run).Should().Be(
                "bool           <Boolean>true</Boolean>\n" +
                "byte           <Byte>255</Byte>\n" +
                "sbyte          <SByte>-128</SByte>\n" +
                "short          <Int16>-32768</Int16>\n" +
                "ushort         <UInt16>65535</UInt16>\n" +
                "int            <Int32>-42</Int32>\n" +
                "uint           <UInt32>4294967295</UInt32>\n" +
                "long           <Int64>-9223372036854775808</Int64>\n" +
                "ulong          <UInt64>18446744073709551615</UInt64>\n" +
                "float          <Single>3.14</Single>\n" +
                "double         <Double>1E-07</Double>\n" +
                "double         <Double>NaN</Double>\n" +
                "double         <Double>INF</Double>\n" +
                "double         <Double>-INF</Double>\n" +
                "decimal        <Decimal>19.990</Decimal>\n" +
                "string         <String>Fish &amp; chips</String>\n" +
                "char           <Char>A</Char>\n" +
                "Guid           <Guid>0f8fad5b-d9cb-469f-a165-70867728950e</Guid>\n" +
                "DateTime       <DateTime>2026-11-23T14:30:00</DateTime>\n" +
                "DateTime       <DateTime>2026-11-23T14:30:00Z</DateTime>\n" +
                "DateTimeOffset <DateTimeOffset>2026-11-23T14:30:00+02:00</DateTimeOffset>\n" +
                "DateOnly       <DateOnly>2026-11-23</DateOnly>\n" +
                "TimeOnly       <TimeOnly>14:30:00.25</TimeOnly>\n" +
                "TimeSpan       <TimeSpan>P1DT2H3M4S</TimeSpan>\n" +
                "byte[]         <ByteArray>AQIDBAU=</ByteArray>\n" +
                "Uri            <Uri>https://example.com/orders?id=1042&amp;view=full</Uri>\n" +
                "Version        <Version>1.2.3</Version>\n" +
                "enum           <OrderStatus>Shipped</OrderStatus>\n" +
                "[Flags] enum   <DeliveryOptions>Gift Insured</DeliveryOptions>\n" +
                "int?           <Int32>5</Int32>\n" +
                "int?           <Int32 xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:nil=\"true\"/>");
        }

        [TestMethod]
        public void Sample37_SupportedTypes_Collections()
        {
            SampleOutput.Capture(SupportedTypes.Collections).Should().Be(
                "<ListOfInt32><Int32>1</Int32><Int32>2</Int32></ListOfInt32>\n" +
                "<StringArray><String>red</String><String>green</String></StringArray>\n" +
                "<DictionaryOfStringInt32><apples>3</apples><Key>ripe pears</Key><Value>5</Value></DictionaryOfStringInt32>\n" +
                "<DictionaryOfInt32String><Key>1042</Key><Value>Keyboard</Value></DictionaryOfInt32String>");
        }

        [TestMethod]
        public void Sample37_SupportedTypes_Enums()
        {
            SampleOutput.Capture(SupportedTypes.Enums).Should().Be(
                "<OrderStatus>Shipped</OrderStatus>\n" +
                "<OrderStatus>1</OrderStatus>\n" +
                "<DeliveryOptions>Gift Insured</DeliveryOptions>\n" +
                "<DeliveryOptions>5</DeliveryOptions>\n" +
                "True");
        }

        [TestMethod]
        public void Sample37_SupportedTypes_ReadLexicalForms()
        {
            SampleOutput.Capture(SupportedTypes.ReadLexicalForms).Should().Be(
                "True\n" +
                "42\n" +
                "True\n" +
                "0f8fad5b-d9cb-469f-a165-70867728950e\n" +
                "Utc\n" +
                "01:30:00\n" +
                "0102030405\n" +
                "Delivered\n" +
                "The value is not a valid decimal. Path: /Decimal | LineNumber: 1 | BytePositionInLine: 24.");
        }

        [TestMethod]
        public void Sample37_SupportedTypes_UnsupportedType()
        {
            SampleOutput.Capture(SupportedTypes.UnsupportedType).Should().Be(
                "The type 'System.IntPtr' is not supported.\n" +
                "The type 'System.Int32[,]' is not supported.");
        }

        [TestMethod]
        public void Sample38_XmlConformance()
        {
            SampleOutput.Capture(XmlConformance.Run).Should().Be(
                "XML 1.0: one\\ntwo\\nthree\\u0085four\n" +
                "XML 1.1: one\\ntwo\\nthree\\nfour");
        }

        [TestMethod]
        public void Sample38_XmlConformance_CompareVersions()
        {
            SampleOutput.Capture(XmlConformance.CompareVersions).Should().Be(
                "1.0 control: XmlException at line 1, position 9: Invalid XML 1.0 character.\n" +
                "1.1 control: XmlDeclaration StartElement Text(\"\\u0001\") EndElement\n" +
                "1.0 unbind:  XmlException at line 1, position 35: Prefix undeclaration is not allowed in XML 1.0.\n" +
                "1.1 unbind:  XmlDeclaration StartElement StartElement EndElement");
        }

        [TestMethod]
        public void Sample38_XmlConformance_EscapeOnWrite()
        {
            SampleOutput.Capture(XmlConformance.EscapeOnWrite).Should().Be(
                "<note title=\"say &quot;hi&quot;&#x9;then&#xD;&#xA;leave\">1 &lt; 2 &amp;&amp; ]]&gt; ends&#xD;</note>\n" +
                "True\n" +
                "True");
        }

        [TestMethod]
        public void Sample38_XmlConformance_ExpandReferences()
        {
            SampleOutput.Capture(XmlConformance.ExpandReferences).Should().Be("<p> & \"q\" 'a' AB");
        }

        [TestMethod]
        public void Sample38_XmlConformance_HandleWhitespace()
        {
            SampleOutput.Capture(XmlConformance.HandleWhitespace).Should().Be(
                "Insignificant: StartElement StartElement Text(\" a \") EndElement EndElement\n" +
                "Significant: StartElement Text(\"\\n  \") StartElement Text(\" a \") EndElement Text(\"\\n\") EndElement\n" +
                "None: XmlException at line 2, position 3: Whitespace-only text is not allowed.");
        }

        [TestMethod]
        public void Sample38_XmlConformance_NormalizeAttributeValues()
        {
            SampleOutput.Capture(XmlConformance.NormalizeAttributeValues).Should().Be("a b c\\nd");
        }

        [TestMethod]
        public void Sample38_XmlConformance_RejectUnsupported()
        {
            SampleOutput.Capture(XmlConformance.RejectUnsupported).Should().Be(
                "XmlException at line 1, position 1: DTD is not allowed.\n" +
                "XmlException at line 1, position 13: Undefined entity.\n" +
                "XmlException at line 1, position 44: XmlEssentials is UTF-8 only. Declared encoding 'iso-8859-1' is not supported.");
        }

        [TestMethod]
        public void Sample39_SecurityAndThreadSafety()
        {
            SampleOutput.Capture(SecurityAndThreadSafety.Run).Should().Be(
                "Line 2: DTD is not allowed. Path: /Person | LineNumber: 2 | BytePositionInLine: 1.\n" +
                "Line 1: DTD is not allowed.");
        }

        [TestMethod]
        public void Sample39_SecurityAndThreadSafety_EnforceLimits()
        {
            SampleOutput.Capture(SecurityAndThreadSafety.EnforceLimits).Should().Be(
                "MaxDepth exceeded (64).\n" +
                "MaxTextByteCount exceeded (256). Path: /Person/Name | LineNumber: 1 | BytePositionInLine: 315.\n" +
                "MaxNameByteCount exceeded (64). Path: /Person | LineNumber: 1 | BytePositionInLine: 310.");
        }

        [TestMethod]
        public void Sample39_SecurityAndThreadSafety_FreezeOptions()
        {
            SampleOutput.Capture(SecurityAndThreadSafety.FreezeOptions).Should().Be(
                "Before first use: IsReadOnly = False\n" +
                "After first use:  IsReadOnly = True\n" +
                "This XmlSerializerOptions instance is read-only.\n" +
                "The copy:         IsReadOnly = False\n" +
                "<Person>\n" +
                "  <Name>Ada</Name>\n" +
                "  <Age>36</Age>\n" +
                "</Person>");
        }

        [TestMethod]
        public void Sample39_SecurityAndThreadSafety_LimitDocumentSize()
        {
            SampleOutput.Capture(SecurityAndThreadSafety.LimitDocumentSize).Should().Be("MaxDocumentByteCount exceeded (1024).");
        }

        [TestMethod]
        public void Sample39_SecurityAndThreadSafety_RejectUndeclaredTypes()
        {
            SampleOutput.Capture(SecurityAndThreadSafety.RejectUndeclaredTypes).Should().Be(
                "CourierCarrier\n" +
                "Unknown type discriminator 'System.Diagnostics.Process, System'. Path: /Delivery/Carrier | LineNumber: 2 | BytePositionInLine: 112.");
        }

        [TestMethod]
        public void Sample39_SecurityAndThreadSafety_ShareOptions()
        {
            SampleOutput.Capture(SecurityAndThreadSafety.ShareOptions).Should().Be(
                "<Person><Name>Worker 0</Name><Age>30</Age></Person>\n" +
                "<Person><Name>Worker 1</Name><Age>31</Age></Person>\n" +
                "<Person><Name>Worker 2</Name><Age>32</Age></Person>\n" +
                "<Person><Name>Worker 3</Name><Age>33</Age></Person>");
        }

        #endregion

    }

}
