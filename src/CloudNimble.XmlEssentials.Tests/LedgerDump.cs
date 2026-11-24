using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A generated export of ledger entries, the shape a finance or ERP system dumps: one root, many entries
    /// that carry their fields as attributes, and line items under each.
    /// </summary>
    /// <remarks>
    /// The document and the tokens a reader must report for it are both written from the same values, so
    /// <see cref="Tokens"/> does not depend on any reader. The lines have the format of
    /// <see cref="XmlReaderHarness.AppendTokens"/>.
    /// </remarks>
    internal sealed class LedgerDump
    {

        #region Fields

        private const string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";
        private readonly MemoryStream _bytes = new();
        private readonly bool _indented;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the tokens of the document without the white space between its elements, one line each: what
        /// a reader reports under <c>XmlWhitespaceHandling.Insignificant</c>.
        /// </summary>
        public List<string> DataTokens { get; } = [];

        /// <summary>
        /// Gets, for each entry, the byte offset just after its end tag.
        /// </summary>
        public List<int> EntryEnds { get; } = [];

        /// <summary>
        /// Gets, for each entry, the byte offset just after its start tag.
        /// </summary>
        public List<int> EntryStartTagEnds { get; } = [];

        /// <summary>
        /// Gets, for each entry, the index in <see cref="Tokens"/> of its start tag.
        /// </summary>
        public List<int> EntryTokenIndexes { get; } = [];

        /// <summary>
        /// Gets the tokens of the document, one line each.
        /// </summary>
        public List<string> Tokens { get; } = [];

        /// <summary>
        /// Gets the document as UTF-8.
        /// </summary>
        public byte[] Utf8 { get; private set; } = [];

        #endregion

        #region Constructors

        private LedgerDump(bool indented)
        {
            _indented = indented;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Generates a dump.
        /// </summary>
        /// <param name="entries">The number of entries.</param>
        /// <param name="attributesPerEntry">The number of attributes on an entry.</param>
        /// <param name="linesPerEntry">The number of line items under an entry.</param>
        /// <param name="attributesPerLine">The number of attributes on a line item.</param>
        /// <param name="indented">
        /// <see langword="true"/> to put each element on a line of its own, indented, with CRLF line ends, as
        /// an export written on Windows has them.
        /// </param>
        /// <returns>The dump.</returns>
        public static LedgerDump Create(int entries, int attributesPerEntry, int linesPerEntry, int attributesPerLine, bool indented = false)
        {
            var dump = new LedgerDump(indented);
            dump.Start("l", "Ledger", "urn:erp:ledger", false,
            [
                ("xmlns", "l", XmlnsNamespace, "urn:erp:ledger"),
                (string.Empty, "xmlns", XmlnsNamespace, "urn:erp"),
                ("xmlns", "f", XmlnsNamespace, "urn:fin"),
                (string.Empty, "period", string.Empty, "2026-10"),
            ]);
            for (var i = 0; i < entries; i++)
            {
                dump.Entry(i, attributesPerEntry, linesPerEntry, attributesPerLine);
            }

            dump.Layout(0);
            dump.End("l", "Ledger", "urn:erp:ledger");
            dump.Utf8 = dump._bytes.ToArray();
            return dump;
        }

        /// <summary>
        /// Returns the value of an attribute of an entry.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <param name="index">The attribute, counted from the first.</param>
        /// <returns>The attribute's prefix, local name, namespace URI and value.</returns>
        public static (string Prefix, string Local, string Ns, string Value) EntryAttribute(int entry, int index)
        {
            return index switch
            {
                0 => (string.Empty, "id", string.Empty, "E" + entry.ToString("D7", CultureInfo.InvariantCulture)),
                1 => ("f", "amount", "urn:fin", (entry * 7919L % 10_000_000 / 100m).ToString("F2", CultureInfo.InvariantCulture)),
                2 => (string.Empty, "memo", string.Empty, "Müller & Söhne <" + entry + "> \"q\" €\nnext"),
                _ => (string.Empty, "c" + index, string.Empty, (index % 7 == 0 ? "a&b" : "v") + entry + "-" + index),
            };
        }

        #endregion

        #region Private Methods

        private static string EscapeAttribute(string value)
        {
            // A line feed and the euro sign are written as character references, so the value needs decoding.
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace("\"", "&quot;").Replace("\n", "&#10;").Replace("€", "&#x20AC;");
        }

        private void Layout(int depth)
        {
            if (!_indented)
            {
                return;
            }

            // The reader reports a line end as a line feed, whatever the document has.
            var indent = new string(' ', depth * 2);
            Append("\r\n" + indent);
            Tokens.Add("Text :{} [\n" + indent + "]");
        }

        private static string Name(string prefix, string local)
        {
            return prefix.Length == 0 ? local : prefix + ":" + local;
        }

        private void Append(string xml)
        {
            _bytes.Write(Encoding.UTF8.GetBytes(xml));
        }

        private void End(string prefix, string local, string ns)
        {
            Append("</" + Name(prefix, local) + ">");
            Token("EndElement " + prefix + ":" + local + "{" + ns + "}");
        }

        private void Entry(int entry, int attributeCount, int lineCount, int attributesPerLine)
        {
            if (entry % 100 == 0)
            {
                Layout(1);
                Append("<!-- entry " + entry + " -->");
                Token("Comment :{} [ entry " + entry + " ]");
            }

            var attributes = new List<(string Prefix, string Local, string Ns, string Value)>(attributeCount);
            for (var k = 0; k < attributeCount; k++)
            {
                attributes.Add(EntryAttribute(entry, k));
            }

            Layout(1);
            EntryTokenIndexes.Add(Tokens.Count);
            Start(string.Empty, "Entry", "urn:erp", false, attributes);
            EntryStartTagEnds.Add((int)_bytes.Length);
            for (var j = 0; j < lineCount; j++)
            {
                var line = new List<(string Prefix, string Local, string Ns, string Value)>(attributesPerLine);
                for (var k = 0; k < attributesPerLine; k++)
                {
                    line.Add(k switch
                    {
                        0 => (string.Empty, "n", string.Empty, j.ToString(CultureInfo.InvariantCulture)),
                        1 => ("f", "debit", "urn:fin", "D&C " + entry + "/" + j),
                        _ => (string.Empty, "a" + k, string.Empty, "L" + entry + "-" + j + "-" + k),
                    });
                }

                Layout(2);
                Start(string.Empty, "Line", "urn:erp", true, line);
            }

            Layout(2);
            Start(string.Empty, "Note", "urn:erp", false, []);
            var note = "Note " + entry + ": R&D <dept>\nline two é";
            Append(note.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
            Token("Text :{} [" + note + "]");
            End(string.Empty, "Note", "urn:erp");
            if (entry % 50 == 0)
            {
                Layout(2);
                Start(string.Empty, "Raw", "urn:erp", false, []);
                var raw = "<x> & ]] " + entry;
                Append("<![CDATA[" + raw + "]]>");
                Token("CData :{} [" + raw + "]");
                End(string.Empty, "Raw", "urn:erp");
            }

            Layout(1);
            End(string.Empty, "Entry", "urn:erp");
            EntryEnds.Add((int)_bytes.Length);
        }

        private void Start(string prefix, string local, string ns, bool empty, List<(string Prefix, string Local, string Ns, string Value)> attributes)
        {
            var xml = new StringBuilder("<").Append(Name(prefix, local));
            var token = new StringBuilder("StartElement ").Append(prefix).Append(':').Append(local).Append('{').Append(ns).Append('}').Append(empty ? " empty" : " open");
            foreach (var attribute in attributes)
            {
                xml.Append(' ').Append(Name(attribute.Prefix, attribute.Local)).Append("=\"").Append(EscapeAttribute(attribute.Value)).Append('"');
                token.Append(" @").Append(attribute.Prefix).Append(':').Append(attribute.Local).Append('{').Append(attribute.Ns).Append("}=[").Append(attribute.Value).Append(']');
            }

            Append(xml.Append(empty ? "/>" : ">").ToString());
            Token(token.ToString());
        }

        private void Token(string line)
        {
            Tokens.Add(line);
            DataTokens.Add(line);
        }

        #endregion

    }

}
