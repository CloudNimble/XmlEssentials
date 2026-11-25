using System.Collections.Generic;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Serialization;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 12: serializes lists, arrays, and dictionaries, wrapped and unwrapped.
    /// </summary>
    public static class SerializeCollections
    {

        #region Public Methods

        /// <summary>
        /// Round-trips a <see cref="Playlist"/>, whose list is written as repeated elements with no wrapper.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var playlist = new Playlist { Name = "Focus", Tracks = ["Blue in Green", "Naima", "Nardis"] };
            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n", OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(playlist, options);
            output.WriteLine(xml);

            Playlist? copy = XmlSerializer.Deserialize<Playlist>(xml, options);
            output.WriteLine($"{copy!.Tracks.Count} tracks, last is {copy.Tracks[^1]}");
        }

        /// <summary>
        /// Serializes an <see cref="Album"/>, whose list is written inside a wrapper element with named items.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Wrapped(TextWriter output)
        {
            var album = new Album { Title = "Kind of Blue", Tracks = ["So What", "Freddie Freeloader"] };
            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n", OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(album, options);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Serializes a <see cref="NewsFeed"/>, whose repeated elements have a name that differs from the property name.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ItemNames(TextWriter output)
        {
            var feed = new NewsFeed { Title = "Releases", Entries = ["1.0.0 is out", "1.0.1 fixes a typo"] };
            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n", OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(feed, options);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Serializes a <see cref="Scoreboard"/>, whose wrapped array uses the default item name.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void DefaultItemName(TextWriter output)
        {
            var board = new Scoreboard { Player = "Ada", Scores = [90, 85] };
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            output.WriteLine(XmlSerializer.Serialize(board, options));
        }

        /// <summary>
        /// Shows what is written for an empty and a null collection, wrapped and unwrapped, and what is read back.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void EmptyAndNull(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            output.WriteLine(XmlSerializer.Serialize(new Playlist { Name = "Empty", Tracks = [] }, options));
            output.WriteLine(XmlSerializer.Serialize(new Album { Title = "Empty", Tracks = [] }, options));
            output.WriteLine(XmlSerializer.Serialize(new Album { Title = "Null", Tracks = null }, options));

            Album? empty = XmlSerializer.Deserialize<Album>("<Album><Title>Empty</Title><Tracks /></Album>");
            output.WriteLine($"Wrapped and empty: {empty!.Tracks?.Count.ToString() ?? "null"}");

            Album? missing = XmlSerializer.Deserialize<Album>("<Album><Title>Missing</Title></Album>");
            output.WriteLine($"Wrapper missing: {missing!.Tracks?.Count.ToString() ?? "null"}");
        }

        /// <summary>
        /// Round-trips an <see cref="AppSettings"/>, whose dictionary keys become element names.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Dictionaries(TextWriter output)
        {
            var settings = new AppSettings
            {
                Application = "Storefront",
                Values = new Dictionary<string, string>
                {
                    ["Theme"] = "dark",
                    ["PageSize"] = "20",
                    ["Support email"] = "help@contoso.example",
                },
            };

            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n", OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(settings, options);
            output.WriteLine(xml);

            AppSettings? copy = XmlSerializer.Deserialize<AppSettings>(xml, options);
            output.WriteLine($"{copy!.Values.Count} values, support email is {copy.Values["Support email"]}");
        }

        /// <summary>
        /// Serializes an <see cref="AppSettings"/> with a naming policy for its dictionary keys, and reads the keys back.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void DictionaryKeyPolicy(TextWriter output)
        {
            var settings = new AppSettings
            {
                Application = "Storefront",
                Values = new Dictionary<string, string> { ["PageSize"] = "20" },
            };

            var options = new XmlSerializerOptions
            {
                OmitXmlDeclaration = true,
                DictionaryKeyPolicy = XmlNamingPolicy.CamelCase,
            };

            string xml = XmlSerializer.Serialize(settings, options);
            output.WriteLine(xml);

            AppSettings? copy = XmlSerializer.Deserialize<AppSettings>(xml, options);
            output.WriteLine($"Keys read back: {string.Join(", ", copy!.Values.Keys)}");
        }

        /// <summary>
        /// Round-trips a <see cref="StockReport"/>, whose members are declared with different collection types.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void CollectionTypes(TextWriter output)
        {
            var report = new StockReport
            {
                Counts = [12, 7],
                Warehouses = ["SEA-2", "PDX-1"],
                Skus = ["KB-101", "MS-220"],
                Tags = ["weekly"],
                Notes = ["Recount MS-220"],
            };

            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(report, options);
            output.WriteLine(xml);

            StockReport? copy = XmlSerializer.Deserialize<StockReport>(xml, options);
            output.WriteLine($"Counts: {copy!.Counts!.Length}");
            output.WriteLine($"Warehouses is a List<string>: {copy.Warehouses is List<string>}");
            output.WriteLine($"Skus is a List<string>: {copy.Skus is List<string>}");
            output.WriteLine($"Tags: {copy.Tags!.Count}");
            output.WriteLine($"Notes: {copy.Notes!.Count}");
        }

        /// <summary>
        /// Round-trips a list and a dictionary as the document element.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RootCollections(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            var lines = new List<InvoiceLine>
            {
                new() { Sku = "KB-101", Quantity = 2 },
                new() { Sku = "MS-220", Quantity = 1 },
            };

            string xml = XmlSerializer.Serialize(lines, options);
            output.WriteLine(xml);

            List<InvoiceLine>? copy = XmlSerializer.Deserialize<List<InvoiceLine>>(xml, options);
            output.WriteLine($"{copy!.Count} lines");

            output.WriteLine(XmlSerializer.Serialize(new[] { 1, 2, 3 }, options));
            output.WriteLine(XmlSerializer.Serialize(new Dictionary<string, int> { ["Width"] = 4, ["Height"] = 3 }, options));
        }

        #endregion

    }

}
