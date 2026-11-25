using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using BclXmlDocument = System.Xml.XmlDocument;
using EssentialsXmlNode = CloudNimble.Text.Xml.Nodes.XmlNode;

namespace CloudNimble.XmlEssentials.Performance
{

    /// <summary>
    /// Measures the tree that can be changed (<see cref="EssentialsXmlNode"/>): parsing a catalog into it, and
    /// parsing, changing every item and writing it back.
    /// </summary>
    /// <remarks>
    /// The BCL <see cref="BclXmlDocument"/> is the baseline. <see cref="XDocument"/> and the System.Text.Json
    /// <see cref="JsonNode"/> are beside it, on the same catalog.
    /// </remarks>
    [MemoryDiagnoser]
    [CategoriesColumn]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class XmlNodeBenchmarks
    {

        #region Fields

        private const int Count = 200;
        private XmlWriterSettings _bclSettings = null!;
        private ArrayBufferWriter<byte> _buffer = null!;
        private byte[] _json = null!;
        private Utf8JsonWriter _jsonWriter = null!;
        private MemoryStream _stream = null!;
        private Utf8XmlWriter _writer = null!;
        private byte[] _xml = null!;

        #endregion

        #region Public Methods

        /// <summary>
        /// BCL <see cref="BclXmlDocument"/>: parse, add an attribute and a child to every item, write.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ChangeTree")]
        public long BclXmlDocument_ChangeTree()
        {
            var document = LoadBcl();
            foreach (System.Xml.XmlNode node in document.DocumentElement!.ChildNodes)
            {
                var item = (System.Xml.XmlElement)node;
                item.SetAttribute("checked", "true");
                var stock = document.CreateElement("stock");
                stock.InnerText = "5";
                item.AppendChild(stock);
            }

            _stream.Position = 0;
            _stream.SetLength(0);
            using (var writer = XmlWriter.Create(_stream, _bclSettings))
            {
                document.WriteTo(writer);
            }

            return _stream.Length;
        }

        /// <summary>
        /// BCL <see cref="BclXmlDocument"/>: parse only.
        /// </summary>
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("ParseTree")]
        public int BclXmlDocument_ParseTree()
        {
            return LoadBcl().DocumentElement!.ChildNodes.Count;
        }

        /// <summary>
        /// BCL <see cref="XDocument"/>: parse, add an attribute and a child to every item, write.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ChangeTree")]
        public long BclXDocument_ChangeTree()
        {
            using var input = new MemoryStream(_xml, writable: false);
            var document = XDocument.Load(input);
            foreach (var item in document.Root!.Elements())
            {
                item.SetAttributeValue("checked", "true");
                item.Add(new XElement("stock", "5"));
            }

            _stream.Position = 0;
            _stream.SetLength(0);
            using (var writer = XmlWriter.Create(_stream, _bclSettings))
            {
                document.WriteTo(writer);
            }

            return _stream.Length;
        }

        /// <summary>
        /// BCL <see cref="XDocument"/>: parse only.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ParseTree")]
        public int BclXDocument_ParseTree()
        {
            using var input = new MemoryStream(_xml, writable: false);
            return XDocument.Load(input).Root!.Elements().Count();
        }

        /// <summary>
        /// Returns pooled writer buffers.
        /// </summary>
        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _jsonWriter.Dispose();
            _stream.Dispose();
            _writer.Dispose();
        }

        /// <summary>
        /// Builds the catalog in both formats and the reused destinations.
        /// </summary>
        [GlobalSetup]
        public void GlobalSetup()
        {
            _bclSettings = new XmlWriterSettings { CloseOutput = false, Encoding = new UTF8Encoding(false), OmitXmlDeclaration = true };
            _buffer = new ArrayBufferWriter<byte>(65_536);
            _stream = new MemoryStream(65_536);
            _writer = new Utf8XmlWriter(_buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            _jsonWriter = new Utf8JsonWriter(_buffer);

            var xml = new StringBuilder("<catalog>");
            var json = new StringBuilder("[");
            for (var i = 0; i < Count; i++)
            {
                xml.Append("<item id=\"").Append(i).Append("\"><name>Widget").Append(i).Append("</name><qty>").Append(i).Append("</qty><price>9.99</price></item>");
                json.Append(i == 0 ? "" : ",").Append("{\"id\":").Append(i).Append(",\"name\":\"Widget").Append(i).Append("\",\"qty\":").Append(i).Append(",\"price\":9.99}");
            }

            _xml = Encoding.UTF8.GetBytes(xml.Append("</catalog>").ToString());
            _json = Encoding.UTF8.GetBytes(json.Append(']').ToString());
        }

        /// <summary>
        /// System.Text.Json <see cref="JsonNode"/>: parse, add two properties to every item, write.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ChangeTree")]
        public long JsonNode_ChangeTree()
        {
            var array = JsonNode.Parse(_json)!.AsArray();
            foreach (var item in array)
            {
                var value = item!.AsObject();
                value["checked"] = true;
                value["stock"] = 5;
            }

            _buffer.ResetWrittenCount();
            _jsonWriter.Reset(_buffer);
            array.WriteTo(_jsonWriter);
            _jsonWriter.Flush();
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// System.Text.Json <see cref="JsonNode"/>: parse only. The nodes of the items are made when they are first asked for.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ParseTree")]
        public int JsonNode_ParseTree()
        {
            return JsonNode.Parse(_json)!.AsArray().Count;
        }

        /// <summary>
        /// XmlEssentials <see cref="EssentialsXmlNode"/>: parse, add an attribute and a child to every item, write.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ChangeTree")]
        public long XmlEssentials_ChangeTree()
        {
            var root = (XmlElementNode)EssentialsXmlNode.Parse(_xml);
            var children = root.Children;
            for (var i = 0; i < children.Count; i++)
            {
                var item = (XmlElementNode)children[i];
                item.SetAttribute("checked", "true");
                item.AddElement("stock").Add(new XmlTextNode("5"));
            }

            _buffer.ResetWrittenCount();
            _writer.Reset(_buffer);
            root.WriteTo(_writer);
            _writer.Flush();
            return _buffer.WrittenCount;
        }

        /// <summary>
        /// XmlEssentials <see cref="EssentialsXmlNode"/>: parse only.
        /// </summary>
        [Benchmark]
        [BenchmarkCategory("ParseTree")]
        public int XmlEssentials_ParseTree()
        {
            return ((XmlElementNode)EssentialsXmlNode.Parse(_xml)).Children.Count;
        }

        #endregion

        #region Private Methods

        private BclXmlDocument LoadBcl()
        {
            using var input = new MemoryStream(_xml, writable: false);
            var document = new BclXmlDocument();
            document.Load(input);
            return document;
        }

        #endregion

    }

}
