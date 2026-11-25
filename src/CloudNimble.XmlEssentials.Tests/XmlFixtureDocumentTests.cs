using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Real documents from other parsers' test suites (see <c>Fixtures/README.md</c>).
    /// </summary>
    /// <remarks>
    /// The expected counts and SHA-256 digests were produced once by an independent reader
    /// (<c>System.Xml.XmlReader</c>, in a script outside this repository's runtime and tests) and pinned
    /// here. A digest covers every element name with its namespace, every attribute with its namespace
    /// and normalized value, or all character data, in document order.
    /// </remarks>
    [TestClass]
    public class XmlFixtureDocumentTests
    {

        #region Fields

        private static readonly XmlReaderOptions ReaderOptions = new()
        {
            CommentHandling = XmlCommentHandling.Allow,
            WhitespaceHandling = XmlWhitespaceHandling.Significant,
        };

        #endregion

        #region Public Methods

        [TestMethod]
        [DataRow("quick-xml/document.xml", 342, 480, 0, 0, 16L, "BE1CA0476297ED26C11B6E99EEA6DC87975A507EE4DBBEA11AF8C357383A0D8E", "BB19E0815034CBD58C13858A455C059FCBD6DCF4C5D40C1CB94B78B0B8B76C6A", "0214F8FF8F7361495BEFB1E395FAB0DA8B29388248719B3FB03DE39E3AD8F0F0")]
        [DataRow("quick-xml/libreoffice_document.fodt", 659, 976, 0, 0, 14925L, "DD657D3E4AD664377C7A8A2501105A3F8BF364E6A5BB6DCAE125405FED0ACAA9", "4C0B0F54B85FFFE4AB191659AF4844CE35EE25F63FEF67D6A5BC92AC679AF66A", "D64A5DD4251E70E5F5205EEB001673F96C83D1609243C12E96A1D0DBF72A17AD")]
        [DataRow("quick-xml/linescore.xml", 11, 130, 1, 0, 41L, "CE74C0D600B258F3F5514D442E994FB5863FEE3DFEFFCFF84E8D02EC6E92DBB3", "9ADE3AC8D2E9B442284C87DC1CD006F2ED301560F53AF617653171EBF2092DA6", "8550D6328D8C06008DCC9DBA6FF2EF0FD73B0B5D427EEA41750BAECE7B0CDBD3")]
        [DataRow("quick-xml/players.xml", 76, 1049, 1, 0, 159L, "B1C5D580A3A480920CAB33BCD2A660299B8E80133AE4E426FE6AD8C3B91E93FF", "4817FB78AC672BCB2EBC636EAF67212809883D1AB1195AAA09B0C06D07AC20C4", "C7BF301F52DD5058078D825DC64709F5A25A16C9DF1F5B59A3795923702AC3DF")]
        [DataRow("quick-xml/rpm_filelists.xml", 184, 100, 0, 0, 6998L, "45C2066B043E05200FC75450E2FC67FC3F2ACFC1454A7D6863A6C3D709339AF2", "DA3DE0EFAE85771B76143FD08342126860F7F0289B840D68C5DBBA08B16109D1", "F9A59091CC228F715F61CE9ECEE6230BE7FC2162507F1BB7266DB60FF29B9057")]
        [DataRow("quick-xml/rpm_other.xml", 145, 314, 0, 0, 7933L, "63DE599F0A6DB5BB8E72A4EF66B41D8F6C82F0B1D39C634ACCBBB5D87E0B1EE4", "4630869D39C0CD8F272C1C29E49DC62F5C4F527F441810A434A91DC98AC6BDC0", "C78101EA8A91F504B8C7235DCFC7E5C214D2BF8FE0A1EAACE78F1837B16B4ED5")]
        [DataRow("quick-xml/rpm_primary.xml", 369, 435, 0, 0, 7003L, "1333D532757BB0B798E0E6324D467EAA4BCFAD4D9FD318BD501341DDFF8B2375", "BEC48BF5A63135327DE66C10A38E7ACB67FC90F5328C1D4E796851AA37F862E4", "2A623F9F0291CCA1B6A0F217DD8422B3772BF18194A2FE9CE3A1AF292245A859")]
        [DataRow("quick-xml/rpm_primary2.xml", 116, 142, 0, 0, 3148L, "39207D39AC3D76C5096159794ACAC6BB6C2AB76153EDC75454F98B12AEA709D8", "1D35A234F6D432AEEC4EF1DF3C69886BB1172BEDB859F5F14AC58052981570FE", "D726214C78DB8D4C3605797DFAB0CC0A9C9CE8F60020775E88ECB6BF9520E0A5")]
        [DataRow("quick-xml/sample_1.xml", 15, 22, 0, 0, 436L, "FA81AF24777727733F4D8A460CCF3E174B4406EA0A69ADB24DB2D1D3911A3130", "86563ECD1F368BDF92FEA0891BEC7D8A0579F880672B77A0CEAA16EA8246A071", "35C60A07B90694F45E82B4291F867CFBF546ACD81892C6382B36E603E4181455")]
        [DataRow("quick-xml/sample_config.xml", 71, 71, 0, 0, 878L, "2324D724650793B95DAE76D386A288BA6F2ABCBB8E4F288A3CD3322052CA0BD1", "A5B02855102F30695B647282C00F138C0AC64BE07CF8328861A460CCD49D3495", "F6C3A53C9E1A720B57AE28D91311756EC8FE509EEFB51722B7F195CCF0610786")]
        [DataRow("quick-xml/sample_ns.xml", 11, 20, 0, 0, 117L, "222076F8E4A328B59539027459273E056A17A0DE6DC5141EAD78B8EF9CBD2925", "1BAAC0CF175CB386DD1163682FE04A156F0FB4BA60DCF1CA4D7F141D91D01E10", "499D35165FA840B6F4CBCF5113403746C17FF268DC108F8848E14EF3A7016CB8")]
        [DataRow("quick-xml/sample_rss.xml", 1550, 813, 0, 2, 121918L, "CEC9C7A08A2E7249BED9F397A03801694E45428EDBC070161B660523740FDDC7", "B2514B134CCE6EF801340F3301C906EA2F33551A7154D7C2815CF80D5F0E15AB", "C1EE494FADF71110089A986738C4CFA53C1A7F5197F7DFAA33CEC4F65E104CE6")]
        [DataRow("quick-xml/test_writer_indent.xml", 34, 78, 1, 0, 838L, "C26C17E3FD51D82EE737C218739C5300A1F72F8834470BFEC0FE9A7E6AC11441", "2ABA53D5767FFF988DB480E9B49ACE10E13A85F5DB9DB7DE79A5096FA5EEF453", "F680448C26D406008B86C46E92E4518F730123FDEEB185E8AA0598DF24631478")]
        [DataRow("captivate/whatsyourstorypod.rss", 1129, 568, 0, 0, 490300L, "7EE2A9A7F804FA6F56C52AB8CA5091813D0F43074AA657F7C44D93B51B08C131", "A11DD25066E0DCBE540A2901C2AFBFF935DF5C450D8266ED77290598015CFC96", "D752FF268FB02E28CB8A33B439DDBD0CFEC20F16830BFF8D95764471D90FEBA8")]
        [DataRow("pugixml/large.xml", 10001, 0, 0, 0, 0L, "4AEB329D26630A42AC3341DB7A66D0D30007A24F170F67D7C77F9AAAA220A180", "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855", "E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855")]
        public void RealDocument_MatchesIndependentReader(string file, int elements, int attributes, int comments, int processingInstructions, long textBytes, string nameDigest, string attributeDigest, string textDigest)
        {
            var xml = Read(file);
            var expected = new FixtureMetrics(elements, attributes, comments, processingInstructions, textBytes, nameDigest, attributeDigest, textDigest);

            Measure(xml).Should().Be(expected, "a single pass over {0} must match the independent reader", file);

            var singlePass = XmlReaderHarness.Tokens(xml, ReaderOptions);
            foreach (var chunkSize in new[] { 1, 7, 251, 4096 })
            {
                if (chunkSize == 1 && xml.Length > 20_000)
                {
                    continue;
                }

                var chunked = XmlReaderHarness.TokensChunked(xml, chunkSize, ReaderOptions);
                chunked.Count.Should().Be(singlePass.Count, "{0} read in {1}-byte chunks must produce the same tokens", file, chunkSize);
                for (var i = 0; i < singlePass.Count; i++)
                {
                    chunked[i].Should().Be(singlePass[i], "token {0} of {1} must not depend on {2}-byte chunks", i, file, chunkSize);
                }
            }

            var rewritten = WriteThroughDocument(xml);
            Measure(rewritten).Should().Be(expected, "{0} written back by XmlDocument.WriteTo must keep its infoset", file);
            WriteThroughDocument(rewritten).AsSpan().SequenceEqual(rewritten).Should().BeTrue("writing {0} a second time must be byte-stable", file);
        }

        [TestMethod]
        [DataRow("quick-xml/opennews_all.rss", "XmlEssentials is UTF-8 only. Declared encoding 'koi8-r' is not supported.")]
        [DataRow("pugixml/latintest_utf8.xml", "XmlEssentials is UTF-8 only. Declared encoding 'iso-8859-1' is not supported.")]
        [DataRow("pugixml/utftest_utf8.xml", "DTD is not allowed.")]
        [DataRow("pugixml/utftest_utf8_bom.xml", "DTD is not allowed.")]
        [DataRow("pugixml/utftest_utf8_nodecl.xml", "DTD is not allowed.")]
        public void RealDocument_OutsideTheSupportedSubset_IsRejected(string file, string message)
        {
            var xml = Read(file);
            string? actual = null;
            try
            {
                XmlReaderHarness.Tokens(xml, ReaderOptions);
            }
            catch (XmlException ex)
            {
                actual = ex.Message;
            }

            actual.Should().Be(message);
            XmlDocument.TryParse(xml, default, out var document).Should().BeFalse();
            document.Should().BeNull();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Returns the bytes of a fixture file.
        /// </summary>
        /// <param name="relativePath">The path under <c>Fixtures</c>, with forward slashes.</param>
        /// <returns>The file content.</returns>
        internal static byte[] Read(string relativePath)
        {
            return File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        /// <summary>
        /// Parses <paramref name="xml"/> into an <see cref="XmlDocument"/> and writes it back out.
        /// </summary>
        /// <param name="xml">The document.</param>
        /// <param name="version">The XML version to write.</param>
        /// <returns>The rewritten bytes.</returns>
        internal static byte[] WriteThroughDocument(ReadOnlySpan<byte> xml, XmlVersion version = XmlVersion.Version10)
        {
            using var document = XmlDocument.Parse(xml, new XmlDocumentOptions
            {
                CommentHandling = XmlCommentHandling.Allow,
                WhitespaceHandling = XmlWhitespaceHandling.Significant,
            });
            var buffer = new ArrayBufferWriter<byte>(xml.Length + 64);
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { NewLine = "\n", OmitXmlDeclaration = true, Version = version });
            document.WriteTo(writer);
            writer.Flush();
            return buffer.WrittenSpan.ToArray();
        }

        #endregion

        #region Private Methods

        private static FixtureMetrics Measure(ReadOnlySpan<byte> xml)
        {
            using var names = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var attributes = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var text = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var elementCount = 0;
            var attributeCount = 0;
            var commentCount = 0;
            var processingInstructionCount = 0;
            var textBytes = 0L;
            var reader = new Utf8XmlReader(xml, ReaderOptions);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case XmlTokenType.StartElement:
                        elementCount++;
                        AppendName(names, ref reader);
                        names.AppendData("\n"u8);
                        while (reader.MoveToNextAttribute())
                        {
                            attributeCount++;
                            AppendName(attributes, ref reader);
                            attributes.AppendData("="u8);
                            attributes.AppendData(reader.ValueSpan);
                            attributes.AppendData("\n"u8);
                        }

                        reader.MoveToElement();
                        break;
                    case XmlTokenType.Text:
                    case XmlTokenType.CData:
                        textBytes += reader.ValueSpan.Length;
                        text.AppendData(reader.ValueSpan);
                        break;
                    case XmlTokenType.Comment:
                        commentCount++;
                        break;
                    case XmlTokenType.ProcessingInstruction:
                        processingInstructionCount++;
                        break;
                }
            }

            return new FixtureMetrics(
                elementCount,
                attributeCount,
                commentCount,
                processingInstructionCount,
                textBytes,
                Convert.ToHexString(names.GetHashAndReset()),
                Convert.ToHexString(attributes.GetHashAndReset()),
                Convert.ToHexString(text.GetHashAndReset()));
        }

        private static void AppendName(IncrementalHash hash, ref Utf8XmlReader reader)
        {
            hash.AppendData("{"u8);
            hash.AppendData(reader.NamespaceUriSpan);
            hash.AppendData("}"u8);
            hash.AppendData(reader.LocalNameSpan);
        }

        #endregion

    }

    /// <summary>
    /// What an independent reader reports for a fixture document.
    /// </summary>
    /// <param name="Elements">The number of start tags, empty elements included.</param>
    /// <param name="Attributes">The number of attributes, namespace declarations included.</param>
    /// <param name="Comments">The number of comments.</param>
    /// <param name="ProcessingInstructions">The number of processing instructions.</param>
    /// <param name="TextBytes">The UTF-8 length of all character data inside the root element.</param>
    /// <param name="NameDigest">SHA-256 over <c>{namespace}local</c> lines, one per element.</param>
    /// <param name="AttributeDigest">SHA-256 over <c>{namespace}local=value</c> lines, one per attribute.</param>
    /// <param name="TextDigest">SHA-256 over all character data inside the root element.</param>
    internal sealed record FixtureMetrics(int Elements, int Attributes, int Comments, int ProcessingInstructions, long TextBytes, string NameDigest, string AttributeDigest, string TextDigest);

}
