using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Runs cases from the W3C XML Conformance Test Suite (see <c>Fixtures/README.md</c>): XML 1.0
    /// (James Clark's xmltest, Sun, IBM, and the Edinburgh errata tests), XML 1.1, and Namespaces 1.0 and 1.1.
    /// The manifests are read with <see cref="Utf8XmlReader"/> itself.
    /// </summary>
    /// <remarks>
    /// Expectations follow the manifest <c>TYPE</c>:
    /// <list type="bullet">
    /// <item><description><c>not-wf</c> must throw.</description></item>
    /// <item><description><c>valid</c> and <c>invalid</c> must read: validity is a DTD notion, and this is not a validating parser.</description></item>
    /// <item><description><c>error</c> must throw when it is a fatal error (a NEL or LS in the XML declaration, XML 1.1 section 2.11) and must read when the specification lets a processor recover (an unknown <c>xml:space</c> value, XML 1.0 section 2.10). <see cref="RecoverableErrors"/> lists the second kind.</description></item>
    /// <item><description>A document with a DOCTYPE or an encoding other than UTF-8 is outside the supported subset and must be rejected with the documented message.</description></item>
    /// <item><description>A case whose <c>EDITION</c> does not include 5 tests a rule that XML 1.0 Fifth Edition relaxed. This parser implements the Fifth Edition, so the document must read.</description></item>
    /// </list>
    /// Only the documents this library can judge on well-formedness were copied for the XML 1.0 sections;
    /// a manifest row whose file is absent is skipped. <see cref="ExpectedCases"/> pins how many ran.
    /// </remarks>
    [TestClass]
    public class XmlConformanceSuiteTests
    {

        #region Fields

        private const int ExpectedCases = 283;

        private const int ExpectedInSubset = 205;

        private static readonly string[] Manifests =
        [
            "w3c-xmlconf/xmltest/xmltest.xml",
            "w3c-xmlconf/sun/sun-not-wf.xml",
            "w3c-xmlconf/ibm/ibm_oasis_not-wf.xml",
            "w3c-xmlconf/ibm/xml-1.1/ibm_not-wf.xml",
            "w3c-xmlconf/eduni/errata-2e/errata2e.xml",
            "w3c-xmlconf/eduni/errata-4e/errata4e.xml",
            "w3c-xmlconf/eduni/namespaces/1.0/rmt-ns10.xml",
            "w3c-xmlconf/eduni/namespaces/1.1/rmt-ns11.xml",
            "w3c-xmlconf/eduni/xml-1.1/xml11.xml",
        ];

        private static readonly XmlReaderOptions ReaderOptions = new()
        {
            CommentHandling = XmlCommentHandling.Allow,
            WhitespaceHandling = XmlWhitespaceHandling.Significant,
        };

        private static readonly HashSet<string> RecoverableErrors = ["rmt-e2e-57"];

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets one row per manifest test whose document is present: id, document path, type, and whether it only applies before the Fifth Edition.
        /// </summary>
        public static IEnumerable<object[]> Cases
        {
            get
            {
                foreach (var manifest in Manifests)
                {
                    var directory = manifest[..(manifest.LastIndexOf('/') + 1)];
                    foreach (var (id, uri, type, edition) in ReadManifest(XmlFixtureDocumentTests.Read(manifest)))
                    {
                        if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "Fixtures", (directory + uri).Replace('/', Path.DirectorySeparatorChar))))
                        {
                            continue;
                        }

                        yield return [id, directory + uri, type, edition.Length > 0 && !edition.Contains('5', StringComparison.Ordinal)];
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        [TestMethod]
        public void Manifests_CaseCounts_AreKnown()
        {
            var total = 0;
            var inSubset = 0;
            foreach (var row in Cases)
            {
                total++;
                if (Classify(XmlFixtureDocumentTests.Read((string)row[1])) is null)
                {
                    inSubset++;
                }
            }

            total.Should().Be(ExpectedCases);
            inSubset.Should().Be(ExpectedInSubset);
        }

        [TestMethod]
        [DynamicData(nameof(Cases))]
        public void Case_MatchesManifest(string id, string file, string type, bool preFifthEdition)
        {
            var xml = XmlFixtureDocumentTests.Read(file);
            string? error = null;
            List<string>? tokens = null;
            XmlVersion version = default;
            try
            {
                tokens = XmlReaderHarness.Tokens(xml, ReaderOptions);
                var reader = new Utf8XmlReader(xml, ReaderOptions);
                reader.Read();
                version = reader.Version;
            }
            catch (XmlException ex)
            {
                error = ex.Message;
            }

            var outside = Classify(xml);
            if (outside is not null)
            {
                error.Should().BeOneOf(outside, "{0} uses a DTD or an encoding other than UTF-8", id);
                return;
            }

            var mustReject = type is "not-wf" || (type is "error" && !RecoverableErrors.Contains(id));
            if (mustReject && !preFifthEdition)
            {
                error.Should().NotBeNull("{0} is {1} and must be rejected", id, type);
                XmlDocument.TryParse(xml, default, out _).Should().BeFalse("{0} must be rejected by the DOM too", id);
                return;
            }

            error.Should().BeNull("{0} is {1} and must be read", id, type);
            foreach (var chunkSize in new[] { 1, 3 })
            {
                XmlReaderHarness.TokensChunked(xml, chunkSize, ReaderOptions).Should().Equal(tokens, "{0} must not depend on {1}-byte chunks", id, chunkSize);
            }

            var rewritten = XmlFixtureDocumentTests.WriteThroughDocument(xml, version);
            var reread = XmlReaderHarness.Tokens(rewritten, ReaderOptions);
            WithoutDeclaration(reread).Should().Equal(WithoutDeclaration(tokens!), "{0} must survive XmlDocument.WriteTo", id);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Returns the messages a suite document may be rejected with when it is outside the supported subset,
        /// or <see langword="null"/> when it is UTF-8 without a DTD and must be judged on well-formedness.
        /// </summary>
        /// <param name="xml">The document bytes.</param>
        /// <returns>The allowed rejection messages, or <see langword="null"/>.</returns>
        private static string[]? Classify(ReadOnlySpan<byte> xml)
        {
            var declared = DeclaredEncoding(xml);
            var foreignEncoding = declared is not null && declared is not ("utf-8" or "utf8");
            if (System.Text.Unicode.Utf8.IsValid(xml) && xml.IndexOf("<!DOCTYPE"u8) < 0 && !foreignEncoding)
            {
                return null;
            }

            return
            [
                "DTD is not allowed.",
                "Invalid UTF-8.",
                "The XML declaration is malformed.",
                "XmlEssentials is UTF-8 only. Declared encoding '" + declared + "' is not supported.",
            ];
        }

        /// <summary>
        /// Returns the lower-cased encoding name in the XML declaration, or <see langword="null"/> when none is declared.
        /// </summary>
        /// <param name="xml">The document bytes.</param>
        /// <returns>The declared encoding.</returns>
        private static string? DeclaredEncoding(ReadOnlySpan<byte> xml)
        {
            var head = Encoding.Latin1.GetString(xml[..Math.Min(xml.Length, 120)]);
            var end = head.IndexOf("?>", StringComparison.Ordinal);
            var at = head.IndexOf("encoding", StringComparison.Ordinal);
            if (!head.StartsWith("<?xml", StringComparison.Ordinal) || end < 0 || at < 0 || at > end)
            {
                return null;
            }

            var open = head.IndexOfAny(['"', '\''], at);
            var close = open < 0 ? -1 : head.IndexOf(head[open], open + 1);
            return close < 0 ? null : head[(open + 1)..close].ToLowerInvariant();
        }

        /// <summary>
        /// Reads the <c>TEST</c> elements of a manifest, which may be a document or a rootless entity.
        /// </summary>
        /// <param name="manifest">The manifest bytes.</param>
        /// <returns>Id, URI, type, and edition list (empty when unrestricted) of each test.</returns>
        private static List<(string Id, string Uri, string Type, string Edition)> ReadManifest(byte[] manifest)
        {
            var tests = new List<(string, string, string, string)>();
            var reader = new Utf8XmlReader(manifest, new XmlReaderOptions { AllowMultipleRoots = true });
            while (reader.Read())
            {
                if (reader.TokenType is not XmlTokenType.StartElement || !reader.LocalNameEquals("TEST"))
                {
                    continue;
                }

                string id = string.Empty, uri = string.Empty, type = string.Empty, edition = string.Empty;
                while (reader.MoveToNextAttribute())
                {
                    if (reader.LocalNameEquals("ID"))
                    {
                        id = reader.GetString()!;
                    }
                    else if (reader.LocalNameEquals("URI"))
                    {
                        uri = reader.GetString()!;
                    }
                    else if (reader.LocalNameEquals("TYPE"))
                    {
                        type = reader.GetString()!;
                    }
                    else if (reader.LocalNameEquals("EDITION"))
                    {
                        edition = reader.GetString()!;
                    }
                }

                reader.MoveToElement();
                tests.Add((id, uri, type, edition));
            }

            return tests;
        }

        private static List<string> WithoutDeclaration(List<string> tokens)
        {
            return tokens.FindAll(static t => !t.StartsWith(nameof(XmlTokenType.XmlDeclaration), StringComparison.Ordinal));
        }

        #endregion

    }

}
