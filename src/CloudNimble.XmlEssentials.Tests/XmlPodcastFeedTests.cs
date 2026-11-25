using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Tests.POCOs.Podcast;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A real podcast feed (<c>Fixtures/captivate/whatsyourstorypod.rss</c>, 56 episodes): read with
    /// <see cref="Utf8XmlReader"/>, copied through <see cref="Utf8XmlWriter"/>, and read and written by
    /// <see cref="XmlSerializer"/> with the types in <c>POCOs/Podcast</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every value is checked against <c>whatsyourstorypod.values.txt</c>: one line for each attribute and each
    /// element with text, as <c>path=value</c>, sorted. That file was written by <c>System.Xml.Linq</c> in a
    /// script outside this repository's runtime and tests, so it does not depend on this library. A path names
    /// each element with the prefix the feed uses and its position among the siblings of the same name; a line
    /// feed in a value is written as a backslash and <c>n</c>.
    /// </para>
    /// <para>
    /// <c>XmlFixtureDocumentTests</c> pins the counts and digests of the same file, reads it in blocks, and
    /// writes it back through <see cref="XmlDocument"/>.
    /// </para>
    /// </remarks>
    [TestClass]
    public class XmlPodcastFeedTests
    {

        #region Fields

        private const string Declaration = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>";
        private const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";

        private static readonly Lazy<string[]> ExpectedValues = new(() =>
            Encoding.UTF8.GetString(XmlFixtureDocumentTests.Read("captivate/whatsyourstorypod.values.txt")).Split('\n', StringSplitOptions.RemoveEmptyEntries));

        private static readonly Lazy<byte[]> Feed = new(() => XmlFixtureDocumentTests.Read("captivate/whatsyourstorypod.rss"));

        private static readonly XmlSerializerOptions Options = new() { NewLine = "\n", OmitXmlDeclaration = true };

        #endregion

        #region Public Methods

        [TestMethod]
        public void Reader_ReportsEveryValueOfTheFeed()
        {
            ExpectedValues.Value.Length.Should().Be(1364);

            ShouldBeTheSameLines(ValuesOf(Feed.Value), ExpectedValues.Value);
        }

        [TestMethod]
        public void ReaderToWriter_CopiesTheFeed_ChangingOnlyTheFormsTheWriterDoesNotChoose()
        {
            var feed = Feed.Value;
            var copy = Copy(feed);

            // The writer names the encoding in lower case, and writes a quotation mark or an apostrophe as
            // itself wherever it does not end a value. Nothing else differs: the prefixes, the place of each
            // namespace declaration, the CDATA sections and <itunes:category ...></itunes:category> are as the
            // feed has them.
            var text = Encoding.UTF8.GetString(feed);
            text[..Declaration.Length].Should().Be(Declaration);
            var expected = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" + WithQuotesAsTheWriterWritesThem(text[Declaration.Length..]);
            ShouldBeTheSameText(Encoding.UTF8.GetString(copy), expected);

            ShouldBeTheSameLines(ValuesOf(copy), ExpectedValues.Value);
            Copy(copy).AsSpan().SequenceEqual(copy).Should().BeTrue("a copy of the copy is the same bytes");
        }

        [TestMethod]
        public void Serializer_ReadsEveryValueOfTheFeed()
        {
            var rss = XmlSerializer.Deserialize<PodcastRss>(Feed.Value, Options)!;

            ShouldBeTheSameLines(ValuesOf(rss), ExpectedValues.Value);

            // The values that have a type of their own.
            var channel = rss.Channel!;
            channel.Guid.Should().Be(new Guid("053f6415-57bf-5532-aace-a0daea5bcdeb"));
            channel.LastBuildDate.Should().Be(new DateTimeOffset(2026, 11, 12, 15, 50, 53, TimeSpan.Zero));
            channel.Explicit.Should().BeFalse();
            channel.Episodes.Count.Should().Be(56);
            channel.Episodes.Count(episode => episode.Explicit).Should().Be(ExpectedValues.Value.Count(line => line.EndsWith("/itunes:explicit[1]=true", StringComparison.Ordinal)));

            var newest = channel.Episodes[0];
            newest.PublishedOn.Should().Be(new DateTimeOffset(2026, 11, 4, 6, 0, 0, TimeSpan.FromHours(-5)));
            newest.Enclosure!.Length.Should().Be(40_994_419L);
            newest.Season.Should().Be(2);
            newest.Episode.Should().Be(5);
            newest.Guid!.IsPermaLink.Should().BeFalse();
            newest.AlternateEnclosures.Select(alternate => alternate.Height).Should().Equal(null, 720);
            newest.Transcripts.Select(transcript => transcript.Rel).Should().Equal("captions", null);

            var oldest = channel.Episodes[^1];
            oldest.PublishedOn.Should().Be(new DateTimeOffset(2025, 10, 4, 15, 0, 0, TimeSpan.FromHours(-4)));
            oldest.Season.Should().Be(1);
            oldest.Episode.Should().Be(1);
            oldest.Transcripts.Should().BeEmpty();
            oldest.AlternateEnclosures.Should().BeEmpty();
        }

        [TestMethod]
        public void Serializer_WritesTheFeedBack_WithEveryValueAndEveryEpisodeAsItWas()
        {
            var feed = Feed.Value;
            var rss = XmlSerializer.Deserialize<PodcastRss>(feed, Options)!;
            var written = XmlSerializer.SerializeToUtf8Bytes(rss, Options);

            // Every value is where it was, by element name and namespace.
            ShouldBeTheSameLines(ValuesOf(written), ExpectedValues.Value);

            // Every episode has the same elements, attributes, text and CDATA sections, in the same order.
            // The serializer chooses how a namespace is declared, so names are compared by namespace, not prefix.
            var original = ContentOf(feed);
            var rewritten = ContentOf(written);
            var firstItem = original.IndexOf("<{}item");
            ShouldBeTheSameLines(rewritten.Skip(rewritten.IndexOf("<{}item")).ToArray(), original.Skip(firstItem).ToArray());

            // The channel's own elements are in the feed's order too, but for one: the feed has its second
            // atom:link after <link>, and a list of links is written in one place.
            var header = original.Take(firstItem).ToList();
            var hub = header.FindIndex(line => line.Contains("{}rel=[hub]", StringComparison.Ordinal));
            var self = header.FindIndex(line => line.Contains("{}rel=[self]", StringComparison.Ordinal));
            var moved = header.GetRange(hub, 2);
            header.RemoveRange(hub, 2);
            header.InsertRange(self + 2, moved);
            ShouldBeTheSameLines(rewritten.Take(firstItem).ToArray(), [.. header]);

            // What was written reads back as the same feed, and is written the same way again.
            var again = XmlSerializer.Deserialize<PodcastRss>(written, Options)!;
            ShouldBeTheSameLines(ValuesOf(again), ExpectedValues.Value);
            XmlSerializer.SerializeToUtf8Bytes(again, Options).AsSpan().SequenceEqual(written).Should().BeTrue();
        }

        [TestMethod]
        public void Serializer_GivenThePrefixesOfTheFeed_WritesTheFeedsOwnText()
        {
            var feed = Feed.Value;
            var namespaces = XmlDocument.DiscoverNamespaces(feed);
            namespaces.Select(pair => pair.Key + "=" + pair.Value).Should().Equal(
                "dc=http://purl.org/dc/elements/1.1/",
                "content=http://purl.org/rss/1.0/modules/content/",
                "atom=http://www.w3.org/2005/Atom",
                "googleplay=http://www.google.com/schemas/play-podcasts/1.0",
                "itunes=http://www.itunes.com/dtds/podcast-1.0.dtd",
                "sy=http://purl.org/rss/1.0/modules/syndication/",
                "media=http://search.yahoo.com/mrss/",
                "podcast=https://podcastindex.org/namespace/1.0");
            var options = new XmlSerializerOptions { Namespaces = namespaces, NewLine = "\n", OmitXmlDeclaration = true };

            var written = XmlSerializer.Serialize(XmlSerializer.Deserialize<PodcastRss>(feed, options)!, options);

            // The document element declares what the feed's declares, in the feed's order.
            const string start = "<rss xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:content=\"http://purl.org/rss/1.0/modules/content/\""
                + " xmlns:atom=\"http://www.w3.org/2005/Atom\" xmlns:googleplay=\"http://www.google.com/schemas/play-podcasts/1.0\""
                + " xmlns:itunes=\"http://www.itunes.com/dtds/podcast-1.0.dtd\" xmlns:sy=\"http://purl.org/rss/1.0/modules/syndication/\""
                + " xmlns:media=\"http://search.yahoo.com/mrss/\" xmlns:podcast=\"https://podcastindex.org/namespace/1.0\" version=\"2.0\">";

            // The whole document is the feed's own text, character for character, but for the differences
            // that AsTheSerializerWritesIt names.
            ShouldBeTheSameText(written, AsTheSerializerWritesIt(start));
            ShouldBeTheSameLines(ValuesOf(Encoding.UTF8.GetBytes(written)), ExpectedValues.Value);
        }

        [TestMethod]
        public void Serializer_GivenTheKnownPodcastNamespaces_WritesTheCustomaryPrefixes()
        {
            var options = new XmlSerializerOptions { Namespaces = KnownNamespaces.Podcast, NewLine = "\n", OmitXmlDeclaration = true };

            var written = XmlSerializer.Serialize(XmlSerializer.Deserialize<PodcastRss>(Feed.Value, options)!, options);

            const string start = "<rss xmlns:atom=\"http://www.w3.org/2005/Atom\" xmlns:content=\"http://purl.org/rss/1.0/modules/content/\""
                + " xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:googleplay=\"http://www.google.com/schemas/play-podcasts/1.0\""
                + " xmlns:itunes=\"http://www.itunes.com/dtds/podcast-1.0.dtd\" xmlns:media=\"http://search.yahoo.com/mrss/\""
                + " xmlns:podcast=\"https://podcastindex.org/namespace/1.0\" xmlns:psc=\"http://podlove.org/simple-chapters\""
                + " xmlns:sy=\"http://purl.org/rss/1.0/modules/syndication/\" version=\"2.0\">";
            ShouldBeTheSameText(written, AsTheSerializerWritesIt(start));
        }

        #endregion

        #region Private Methods

        private static void Add(List<string> lines, string path, string? value)
        {
            if (value is not null)
            {
                lines.Add(path + "=" + Escape(value));
            }
        }

        /// <summary>
        /// Lists a document as one line a token, with names by namespace and without namespace declarations,
        /// and with an empty-element tag and a start tag followed by an end tag listed alike.
        /// </summary>
        private static List<string> ContentOf(ReadOnlySpan<byte> xml)
        {
            var lines = new List<string>();
            var names = new Stack<string>();
            var reader = new Utf8XmlReader(xml);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case XmlTokenType.StartElement:
                        var name = "{" + Encoding.UTF8.GetString(reader.NamespaceUriSpan) + "}" + Encoding.UTF8.GetString(reader.LocalNameSpan);
                        var line = new StringBuilder("<").Append(name);
                        var empty = reader.IsEmptyElement;
                        while (reader.MoveToNextAttribute())
                        {
                            if (!reader.NamespaceUriEquals(XmlnsNamespace))
                            {
                                line.Append(" {").Append(Encoding.UTF8.GetString(reader.NamespaceUriSpan)).Append('}')
                                    .Append(Encoding.UTF8.GetString(reader.LocalNameSpan)).Append("=[").Append(reader.GetString()).Append(']');
                            }
                        }

                        reader.MoveToElement();
                        lines.Add(line.ToString());
                        if (empty)
                        {
                            lines.Add("</" + name);
                        }
                        else
                        {
                            names.Push(name);
                        }

                        break;
                    case XmlTokenType.EndElement:
                        lines.Add("</" + names.Pop());
                        break;
                    case XmlTokenType.Text:
                        lines.Add("text [" + reader.GetString() + "]");
                        break;
                    case XmlTokenType.CData:
                        lines.Add("cdata [" + reader.GetString() + "]");
                        break;
                }
            }

            return lines;
        }

        /// <summary>
        /// Reads a document and writes each token to a writer as the reader reports it.
        /// </summary>
        private static byte[] Copy(ReadOnlySpan<byte> xml)
        {
            var buffer = new ArrayBufferWriter<byte>(xml.Length);
            // The writer ends a line as its options say, and the feed ends its lines with a line feed.
            var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { NewLine = "\n" });
            var reader = new Utf8XmlReader(xml);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case XmlTokenType.StartElement:
                        writer.WriteStartElement(reader.PrefixSpan, reader.LocalNameSpan, reader.NamespaceUriSpan);
                        var empty = reader.IsEmptyElement;
                        while (reader.MoveToNextAttribute())
                        {
                            var prefix = Encoding.UTF8.GetString(reader.PrefixSpan);
                            var local = Encoding.UTF8.GetString(reader.LocalNameSpan);
                            if (!reader.NamespaceUriEquals(XmlnsNamespace))
                            {
                                writer.WriteAttributeString(prefix.Length == 0 ? null : prefix, local, Encoding.UTF8.GetString(reader.NamespaceUriSpan), reader.GetString());
                            }
                            else if (prefix.Length == 0)
                            {
                                writer.WriteDefaultNamespace(reader.GetString()!);
                            }
                            else
                            {
                                writer.WriteNamespaceDeclaration(local, reader.GetString()!);
                            }
                        }

                        reader.MoveToElement();
                        if (empty)
                        {
                            writer.WriteEndElement();
                        }

                        break;
                    case XmlTokenType.EndElement:
                        writer.WriteFullEndElement();
                        break;
                    case XmlTokenType.Text:
                        writer.WriteStringValue(reader.ValueSpan);
                        break;
                    case XmlTokenType.CData:
                        writer.WriteCData(reader.ValueSpan);
                        break;
                }
            }

            writer.Dispose();
            return buffer.WrittenSpan.ToArray();
        }

        /// <summary>
        /// Returns the feed as the serializer writes it back, given the start tag of the document element: the
        /// feed's own text with these differences, each of which must be found the stated number of times.
        /// </summary>
        private static string AsTheSerializerWritesIt(string documentElementStartTag)
        {
            var text = Encoding.UTF8.GetString(Feed.Value);
            text[..Declaration.Length].Should().Be(Declaration);
            text = WithQuotesAsTheWriterWritesThem(text[Declaration.Length..]);

            // The declarations come before the version attribute, in the order they were given.
            var feedStartTag = text[..(text.IndexOf('>') + 1)];
            text = documentElementStartTag + text[feedStartTag.Length..];

            // The list of atom:link elements is written in one place, so the second moves next to the first.
            const string self = "<atom:link href=\"https://feeds.captivate.fm/whatsyourstorypod/\" rel=\"self\" type=\"application/rss+xml\"/>";
            const string hub = "<atom:link href=\"https://pubsubhubbub.appspot.com\" rel=\"hub\"/>";
            (text.Split(self).Length - 1).Should().Be(1);
            (text.Split(hub).Length - 1).Should().Be(1);
            text = text.Replace(hub, string.Empty).Replace(self, self + hub);

            // An element with no content is written as an empty-element tag. The feed writes two of its
            // categories with a start tag and an end tag.
            const string emptyPair = "\"></itunes:category>";
            (text.Split(emptyPair).Length - 1).Should().Be(2);
            return text.Replace(emptyPair, "\"/>");
        }

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\n", "\\n");
        }

        private static string Number(long value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string PrefixOf(ReadOnlySpan<byte> namespaceUri)
        {
            return Encoding.UTF8.GetString(namespaceUri) switch
            {
                "" => string.Empty,
                PodcastNamespaces.Atom => "atom:",
                PodcastNamespaces.Content => "content:",
                PodcastNamespaces.Itunes => "itunes:",
                PodcastNamespaces.Podcast => "podcast:",
                var other => throw new InvalidOperationException("The feed has no element in namespace '" + other + "'."),
            };
        }

        private static void ShouldBeTheSameLines(string[] actual, string[] expected)
        {
            for (var i = 0; i < Math.Min(actual.Length, expected.Length); i++)
            {
                if (!string.Equals(actual[i], expected[i], StringComparison.Ordinal))
                {
                    actual[i].Should().Be(expected[i], "line {0} must be as the feed has it", i);
                }
            }

            actual.Length.Should().Be(expected.Length);
        }

        private static void ShouldBeTheSameText(string actual, string expected)
        {
            // Compared in pieces so that a difference is reported with the text around it, not as 550,000 characters.
            for (var start = 0; start < Math.Min(actual.Length, expected.Length); start += 200)
            {
                var actualPiece = actual.AsSpan(start, Math.Min(200, actual.Length - start));
                var expectedPiece = expected.AsSpan(start, Math.Min(200, expected.Length - start));
                if (!actualPiece.SequenceEqual(expectedPiece))
                {
                    actualPiece.ToString().Should().Be(expectedPiece.ToString(), "the text from character {0} must be as the feed has it", start);
                }
            }

            actual.Length.Should().Be(expected.Length);
        }

        /// <summary>
        /// Rewrites the two references the feed uses where the writer uses the character: <c>&amp;apos;</c>
        /// anywhere, and <c>&amp;quot;</c> in text. In an attribute value a quotation mark stays a reference,
        /// because it would end the value.
        /// </summary>
        private static string WithQuotesAsTheWriterWritesThem(string xml)
        {
            var result = new StringBuilder(xml.Length);
            var position = 0;
            while (position < xml.Length)
            {
                int end;
                if (xml.AsSpan(position).StartsWith("<![CDATA["))
                {
                    // A CDATA section has no references.
                    end = xml.IndexOf("]]>", position, StringComparison.Ordinal) + 3;
                    result.Append(xml, position, end - position);
                }
                else if (xml[position] is '<')
                {
                    end = xml.IndexOf('>', position) + 1;
                    result.Append(xml[position..end].Replace("&apos;", "'"));
                }
                else
                {
                    end = xml.IndexOf('<', position);
                    result.Append(xml[position..end].Replace("&apos;", "'").Replace("&quot;", "\""));
                }

                position = end;
            }

            return result.ToString();
        }

        /// <summary>
        /// Lists the values of a document as <c>whatsyourstorypod.values.txt</c> lists them, with the reader.
        /// </summary>
        private static string[] ValuesOf(ReadOnlySpan<byte> xml)
        {
            var lines = new List<string>();
            var open = new Stack<(string Path, Dictionary<string, int> Children, StringBuilder? Text)>();
            open.Push((string.Empty, [], null));
            var reader = new Utf8XmlReader(xml);
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case XmlTokenType.StartElement:
                        var parent = open.Peek();
                        var name = PrefixOf(reader.NamespaceUriSpan) + Encoding.UTF8.GetString(reader.LocalNameSpan);
                        parent.Children[name] = parent.Children.GetValueOrDefault(name) + 1;
                        var path = parent.Path + "/" + name + "[" + Number(parent.Children[name]) + "]";
                        var empty = reader.IsEmptyElement;
                        while (reader.MoveToNextAttribute())
                        {
                            if (!reader.NamespaceUriEquals(XmlnsNamespace))
                            {
                                Add(lines, path + "/@" + Encoding.UTF8.GetString(reader.LocalNameSpan), reader.GetString());
                            }
                        }

                        reader.MoveToElement();
                        if (!empty)
                        {
                            open.Push((path, [], null));
                        }

                        break;
                    case XmlTokenType.Text:
                    case XmlTokenType.CData:
                        var current = open.Pop();
                        open.Push((current.Path, current.Children, (current.Text ?? new StringBuilder()).Append(reader.GetString())));
                        break;
                    case XmlTokenType.EndElement:
                        var closed = open.Pop();
                        if (closed.Children.Count == 0)
                        {
                            Add(lines, closed.Path, closed.Text?.ToString());
                        }

                        break;
                }
            }

            lines.Sort(StringComparer.Ordinal);
            return [.. lines];
        }

        /// <summary>
        /// Lists the values of a deserialized feed as <c>whatsyourstorypod.values.txt</c> lists them: each
        /// member under the path of the element or attribute it is mapped to.
        /// </summary>
        private static string[] ValuesOf(PodcastRss rss)
        {
            var lines = new List<string>();
            Add(lines, "/rss[1]/@version", rss.Version);
            var channel = rss.Channel!;
            const string c = "/rss[1]/channel[1]";
            for (var i = 0; i < channel.AtomLinks.Count; i++)
            {
                var link = c + "/atom:link[" + Number(i + 1) + "]";
                Add(lines, link + "/@href", channel.AtomLinks[i].Href);
                Add(lines, link + "/@rel", channel.AtomLinks[i].Rel);
                Add(lines, link + "/@type", channel.AtomLinks[i].Type);
            }

            Add(lines, c + "/title[1]", channel.Title);
            Add(lines, c + "/podcast:guid[1]", channel.Guid.ToString("D"));
            Add(lines, c + "/lastBuildDate[1]", PodcastDateConverter.Format(channel.LastBuildDate));
            Add(lines, c + "/generator[1]", channel.Generator);
            Add(lines, c + "/language[1]", channel.Language);
            Add(lines, c + "/copyright[1]", channel.Copyright);
            Add(lines, c + "/managingEditor[1]", channel.ManagingEditor);
            Add(lines, c + "/itunes:summary[1]", channel.Summary);
            Add(lines, c + "/image[1]/url[1]", channel.Image!.Url);
            Add(lines, c + "/image[1]/title[1]", channel.Image.Title);
            Add(lines, c + "/image[1]/link[1]", channel.Image.Link);
            Add(lines, c + "/itunes:image[1]/@href", channel.Artwork!.Href);
            Add(lines, c + "/itunes:owner[1]/itunes:name[1]", channel.Owner!.Name);
            Add(lines, c + "/itunes:author[1]", channel.Author);
            Add(lines, c + "/description[1]", channel.Description);
            Add(lines, c + "/link[1]", channel.Link);
            Add(lines, c + "/itunes:explicit[1]", channel.Explicit ? "true" : "false");
            Add(lines, c + "/itunes:type[1]", channel.Type);
            for (var i = 0; i < channel.Categories.Count; i++)
            {
                var category = c + "/itunes:category[" + Number(i + 1) + "]";
                Add(lines, category + "/@text", channel.Categories[i].Text);
                for (var j = 0; j < channel.Categories[i].Subcategories.Count; j++)
                {
                    var subcategory = channel.Categories[i].Subcategories[j];
                    subcategory.Subcategories.Should().BeEmpty();
                    Add(lines, category + "/itunes:category[" + Number(j + 1) + "]/@text", subcategory.Text);
                }
            }

            Add(lines, c + "/itunes:new-feed-url[1]", channel.NewFeedUrl);
            Add(lines, c + "/podcast:locked[1]", channel.Locked);
            Add(lines, c + "/podcast:medium[1]", channel.Medium);
            for (var i = 0; i < channel.Episodes.Count; i++)
            {
                var episode = channel.Episodes[i];
                var e = c + "/item[" + Number(i + 1) + "]";
                Add(lines, e + "/title[1]", episode.Title);
                Add(lines, e + "/itunes:title[1]", episode.ItunesTitle);
                Add(lines, e + "/description[1]", episode.Description);
                Add(lines, e + "/content:encoded[1]", episode.Content);
                Add(lines, e + "/link[1]", episode.Link);
                Add(lines, e + "/guid[1]/@isPermaLink", episode.Guid!.IsPermaLink ? "true" : "false");
                Add(lines, e + "/guid[1]", episode.Guid.Value);
                Add(lines, e + "/itunes:image[1]/@href", episode.Artwork!.Href);
                Add(lines, e + "/pubDate[1]", PodcastDateConverter.Format(episode.PublishedOn));
                Add(lines, e + "/enclosure[1]/@url", episode.Enclosure!.Url);
                Add(lines, e + "/enclosure[1]/@length", Number(episode.Enclosure.Length));
                Add(lines, e + "/enclosure[1]/@type", episode.Enclosure.Type);
                Add(lines, e + "/itunes:duration[1]", episode.Duration);
                Add(lines, e + "/itunes:explicit[1]", episode.Explicit ? "true" : "false");
                Add(lines, e + "/itunes:episodeType[1]", episode.EpisodeType);
                Add(lines, e + "/itunes:season[1]", Number(episode.ItunesSeason));
                Add(lines, e + "/itunes:episode[1]", Number(episode.ItunesEpisode));
                Add(lines, e + "/podcast:episode[1]", Number(episode.Episode));
                Add(lines, e + "/podcast:season[1]", Number(episode.Season));
                for (var j = 0; j < episode.Transcripts.Count; j++)
                {
                    var transcript = e + "/podcast:transcript[" + Number(j + 1) + "]";
                    Add(lines, transcript + "/@url", episode.Transcripts[j].Url);
                    Add(lines, transcript + "/@type", episode.Transcripts[j].Type);
                    Add(lines, transcript + "/@rel", episode.Transcripts[j].Rel);
                }

                for (var j = 0; j < episode.AlternateEnclosures.Count; j++)
                {
                    var alternate = e + "/podcast:alternateEnclosure[" + Number(j + 1) + "]";
                    Add(lines, alternate + "/@type", episode.AlternateEnclosures[j].Type);
                    Add(lines, alternate + "/@title", episode.AlternateEnclosures[j].Title);
                    Add(lines, alternate + "/@height", episode.AlternateEnclosures[j].Height is { } height ? Number(height) : null);
                    Add(lines, alternate + "/podcast:source[1]/@uri", episode.AlternateEnclosures[j].Source!.Uri);
                }
            }

            lines.Sort(StringComparer.Ordinal);
            return [.. lines];
        }

        #endregion

    }

}
