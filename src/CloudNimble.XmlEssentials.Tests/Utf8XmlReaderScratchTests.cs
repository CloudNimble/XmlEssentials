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
    /// A reader given a scratch buffer reports the same tokens as one without, whatever the buffer's size or
    /// contents, and allocates nothing while the buffer lasts.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class Utf8XmlReaderScratchTests
    {

        #region Fields

        /// <summary>
        /// A document that needs every kind of working memory more than once: twelve and then twenty
        /// attributes on one tag, decoded attribute values that outgrow their first buffer, eleven and then
        /// nineteen namespaces in scope, elements twenty deep with namespaces declared below the eighth level,
        /// and decoded text and comments that keep getting longer.
        /// </summary>
        internal const string Demanding =
            "<r xmlns:a='urn:a' xmlns:b='urn:b' xmlns:c='urn:c' xmlns:d='urn:d' xmlns:e='urn:e' xmlns:f='urn:f' xmlns:g='urn:g' xmlns:h='urn:h' xmlns:i='urn:i' xmlns:j='urn:j' xmlns:k='urn:k' v='1&amp;2'>" +
            "<!--c\r\nd-->t&lt;1" +
            "<a:x a1='1' a2='2' a3='3' a4='4' a5='5' a6='6' a7='7' a8='8' a9='9' a10='10' a11='&#65;' a12='x\ty'/>" +
            "<b:y b:p='&quot;' xmlns:l='urn:l' xmlns:m='urn:m' xmlns:n='urn:n' xmlns:o='urn:o' xmlns:p='urn:p' xmlns:q='urn:q' xmlns:s='urn:s' xmlns:t='urn:t' c1='1' c2='2' c3='3' c4='4' c5='5' c6='6' c7='7' c8='8' c9='9' c10='10'" +
            " long='a much longer value &amp; one that will not fit where the first decoded values went, because it is well over two hundred and fifty-six bytes once it has been written out in full with its references &lt;here&gt; and &lt;there&gt; and a tab\tand a line end\r\nand more text after that to be sure of it'>" +
            "<l1><l2><l3><l4><l5><l6><l7><t:l8 t:w='&apos;'><l9 xmlns='urn:nine'><l10><l11><l12><l13><l14><l15><l16><l17 xmlns:z='urn:z'><z:l18><l19>deep &amp; deeper\rstill</l19></z:l18></l17></l16></l15></l14></l13></l12></l11></l10></l9></t:l8></l7></l6></l5></l4></l3></l2></l1>" +
            "a longer text than the first one &amp; with a line end\r\nin it</b:y>" +
            "<![CDATA[x\ry]]><?p q\rr?>tail &gt; end, and this is the longest text of them all &amp; so it needs a third buffer</r>";

        #endregion

        #region Public Methods

        [TestMethod]
        public void Read_WithScratchOfEverySize_ReportsTheSameTokens()
        {
            var xml = Encoding.UTF8.GetBytes(Demanding);
            var options = new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow };
            var expected = XmlReaderHarness.Tokens(xml, options);
            expected.Count.Should().Be(50);
            for (var size = 0; size <= 6000; size += size < 600 ? 1 : 97)
            {
                var scratch = new byte[size];
                Array.Fill(scratch, (byte)0xFF);
                Tokens(xml, scratch, options).Should().Equal(expected, "a scratch buffer of {0} bytes must not change what is read", size);
            }
        }

        [TestMethod]
        public void Read_WithScratchAtAnOddAddress_ReportsTheSameTokens()
        {
            var xml = Encoding.UTF8.GetBytes(Demanding);
            var options = new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow };
            var expected = XmlReaderHarness.Tokens(xml, options);
            var backing = new byte[8192];
            for (var offset = 1; offset < 8; offset++)
            {
                Tokens(xml, backing.AsSpan(offset), options).Should().Equal(expected, "a scratch buffer that starts {0} bytes into an array must not change what is read", offset);
            }
        }

        [TestMethod]
        [DataRow(0, DisplayName = "no scratch")]
        [DataRow(64, DisplayName = "scratch that runs out")]
        [DataRow(8192, DisplayName = "scratch that lasts")]
        public void Read_SplitAtEveryByteWithScratch_ReportsTheSameTokens(int size)
        {
            var xml = Encoding.UTF8.GetBytes(Demanding);
            var options = new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow };
            var expected = XmlReaderHarness.Tokens(xml, options);
            var scratch = new byte[size];
            for (var split = 1; split < xml.Length; split++)
            {
                var tokens = new List<string>();
                var first = new Utf8XmlReader(xml.AsSpan(0, split), scratch, isFinalBlock: false, new XmlReaderState(options));
                XmlReaderHarness.AppendTokens(ref first, tokens);
                var second = new Utf8XmlReader(xml.AsSpan((int)first.BytesConsumed), scratch, isFinalBlock: true, first.CurrentState);
                XmlReaderHarness.AppendTokens(ref second, tokens);
                tokens.Should().Equal(expected, "the tokens must not depend on a split at byte {0}", split);
            }
        }

        [TestMethod]
        public void Read_ACopyThatLooksOneTokenAhead_SeesWhatTheOriginalThenReads()
        {
            var xml = Encoding.UTF8.GetBytes(Demanding);
            var options = new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow };
            var scratch = new byte[8192];
            var reader = new Utf8XmlReader(xml, scratch, options);
            var count = 0;
            while (true)
            {
                var ahead = reader;
                var more = ahead.Read();
                var expected = more ? Line(ref ahead) : "";
                reader.Read().Should().Be(more);
                if (!more)
                {
                    break;
                }

                Line(ref reader).Should().Be(expected, "token {0} must read the same after a copy has read it", count);
                count++;
            }

            count.Should().Be(50);
        }


        [TestMethod]
        [DataRow("sample_rss.xml", DisplayName = "RSS: decoded text and a decoded attribute value")]
        [DataRow("libreoffice_document.fodt", DisplayName = "OpenDocument: 38 attributes and 36 namespaces on one tag")]
        [DataRow("document.xml", DisplayName = "WordprocessingML: nine levels, eleven namespaces, twelve attributes")]
        [DataRow("rpm_primary.xml", DisplayName = "RPM metadata: decoded text")]
        public void Drain_RealDocumentWithScratch_AllocatesNothing(string fixture)
        {
            var xml = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Fixtures", "quick-xml", fixture));
            var scratch = new byte[16384];
            var options = new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow, WhitespaceHandling = XmlWhitespaceHandling.Significant };
            var expected = Drain(xml, default, options);
            // Enough passes for every method on the path to reach its final tier, so that nothing the
            // runtime allocates while it recompiles is counted.
            for (var warm = 0; warm < 30; warm++)
            {
                Drain(xml, scratch, options).Should().Be(expected);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            var count = Drain(xml, scratch, options);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            count.Should().Be(expected);
            allocated.Should().Be(0);
        }

        [TestMethod]
        public void Drain_DemandingDocumentWithScratchOnTheStack_AllocatesNothing()
        {
            var xml = Encoding.UTF8.GetBytes(Demanding);
            var options = new XmlReaderOptions { CommentHandling = XmlCommentHandling.Allow };
            DrainOnStack(xml, options).Should().Be(50);
            var before = GC.GetAllocatedBytesForCurrentThread();
            var count = DrainOnStack(xml, options);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            count.Should().Be(50);
            allocated.Should().Be(0);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Counts the tokens of <paramref name="xml"/>.
        /// </summary>
        /// <param name="xml">The document.</param>
        /// <param name="scratch">The reader's scratch buffer, or empty.</param>
        /// <param name="options">Reader options.</param>
        /// <returns>The number of tokens, attributes included.</returns>
        internal static int Drain(byte[] xml, Span<byte> scratch, XmlReaderOptions options)
        {
            var reader = new Utf8XmlReader(xml, scratch, options);
            var count = 0;
            while (reader.Read())
            {
                count++;
                while (reader.MoveToNextAttribute())
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Counts the tokens of <paramref name="xml"/> with a scratch buffer on the stack.
        /// </summary>
        /// <param name="xml">The document.</param>
        /// <param name="options">Reader options.</param>
        /// <returns>The number of tokens.</returns>
        internal static int DrainOnStack(byte[] xml, XmlReaderOptions options)
        {
            Span<byte> scratch = stackalloc byte[8192];
            var reader = new Utf8XmlReader(xml, scratch, options);
            var count = 0;
            while (reader.Read())
            {
                count++;
            }

            return count;
        }

        /// <summary>
        /// Describes the token <paramref name="reader"/> is on.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The token type, its name, its value, and each attribute's name and value.</returns>
        internal static string Line(ref Utf8XmlReader reader)
        {
            var line = new StringBuilder();
            line.Append(reader.TokenType).Append(' ').Append(Encoding.UTF8.GetString(reader.PrefixSpan)).Append(':').Append(Encoding.UTF8.GetString(reader.LocalNameSpan));
            line.Append(" {").Append(Encoding.UTF8.GetString(reader.NamespaceUriSpan)).Append('}');
            if (reader.TokenType is XmlTokenType.StartElement)
            {
                while (reader.MoveToNextAttribute())
                {
                    line.Append(" @").Append(Encoding.UTF8.GetString(reader.LocalNameSpan)).Append("=[").Append(reader.GetString()).Append(']');
                }

                reader.MoveToElement();
            }
            else if (reader.TokenType is not XmlTokenType.EndElement)
            {
                line.Append(" [").Append(reader.GetString()).Append(']');
            }

            return line.ToString();
        }

        /// <summary>
        /// Reads <paramref name="xml"/> with <paramref name="scratch"/> and returns its token lines.
        /// </summary>
        /// <param name="xml">The document.</param>
        /// <param name="scratch">The reader's scratch buffer.</param>
        /// <param name="options">Reader options.</param>
        /// <returns>One line per token.</returns>
        internal static List<string> Tokens(ReadOnlySpan<byte> xml, Span<byte> scratch, XmlReaderOptions options)
        {
            var tokens = new List<string>();
            var reader = new Utf8XmlReader(xml, scratch, options);
            XmlReaderHarness.AppendTokens(ref reader, tokens);
            return tokens;
        }

        #endregion

    }

}
