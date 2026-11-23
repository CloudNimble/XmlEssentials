using System;
using System.Collections.Generic;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Samples.Models.Shapes;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 17: serializes object graphs that hold the same object more than once.
    /// </summary>
    public static class PreserveReferences
    {

        #region Public Methods

        /// <summary>
        /// Serializes a graph with a cycle using <see cref="XmlReferenceHandler.Preserve"/> and reads it back with the
        /// cycle intact.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var tyler = new Employee { Name = "Tyler Stein" };
            var adrian = new Employee { Name = "Adrian King", Manager = tyler };
            tyler.DirectReports.Add(adrian);

            var options = new XmlSerializerOptions
            {
                ReferenceHandler = XmlReferenceHandler.Preserve,
                WriteIndented = true,
                NewLine = "\n",
            };

            string xml = XmlSerializer.Serialize(tyler, options);
            output.WriteLine(xml);

            Employee? copy = XmlSerializer.Deserialize<Employee>(xml, options);
            output.WriteLine(ReferenceEquals(copy, copy!.DirectReports[0].Manager));
        }

        /// <summary>
        /// Serializes a graph with a cycle using <see cref="XmlReferenceHandler.IgnoreCycles"/>, which writes a repeated
        /// object as nil.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void IgnoreCycles(TextWriter output)
        {
            var tyler = new Employee { Name = "Tyler Stein" };
            var adrian = new Employee { Name = "Adrian King", Manager = tyler };
            tyler.DirectReports.Add(adrian);

            var options = new XmlSerializerOptions
            {
                ReferenceHandler = XmlReferenceHandler.IgnoreCycles,
                WriteIndented = true,
                NewLine = "\n",
            };

            string xml = XmlSerializer.Serialize(tyler, options);
            output.WriteLine(xml);

            Employee? copy = XmlSerializer.Deserialize<Employee>(xml, options);
            output.WriteLine(copy!.DirectReports[0].Manager is null);
        }

        /// <summary>
        /// Shows that an object held by two members is one object again after a round trip with
        /// <see cref="XmlReferenceHandler.Preserve"/>, and two objects without it.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ShareOneObject(TextWriter output)
        {
            var dana = new Employee { Name = "Dana Birkby" };
            var project = new Project { Name = "Atlas", Owner = dana, Reviewer = dana };

            var preserve = new XmlSerializerOptions
            {
                ReferenceHandler = XmlReferenceHandler.Preserve,
                OmitXmlDeclaration = true,
                NilHandling = XmlNilHandling.Omit,
            };

            string xml = XmlSerializer.Serialize(project, preserve);
            output.WriteLine(xml);

            Project? shared = XmlSerializer.Deserialize<Project>(xml, preserve);
            output.WriteLine(ReferenceEquals(shared!.Owner, shared.Reviewer));

            var plain = new XmlSerializerOptions { OmitXmlDeclaration = true, NilHandling = XmlNilHandling.Omit };
            xml = XmlSerializer.Serialize(project, plain);
            output.WriteLine(xml);

            Project? copied = XmlSerializer.Deserialize<Project>(xml, plain);
            output.WriteLine(ReferenceEquals(copied!.Owner, copied.Reviewer));
        }

        /// <summary>
        /// Reads a document whose identifiers are resolved by a custom <see cref="XmlReferenceResolver"/>, so that
        /// the caller can look the objects up afterward.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void UseCustomResolver(TextWriter output)
        {
            string xml = """
                <Project xmlns:r="urn:CloudNimble.Text.Xml:refs" r:id="atlas">
                  <Name>Atlas</Name>
                  <Owner r:id="dana">
                    <Name>Dana Birkby</Name>
                  </Owner>
                  <Reviewer r:ref="dana" />
                </Project>
                """;

            var handler = new DirectoryReferenceHandler();
            var options = new XmlSerializerOptions { ReferenceHandler = handler };

            Project? project = XmlSerializer.Deserialize<Project>(xml, options);
            output.WriteLine(ReferenceEquals(project!.Owner, project.Reviewer));

            var dana = (Employee)handler.Directory["dana"];
            output.WriteLine(dana.Name);
            output.WriteLine(string.Join(", ", handler.Directory.Keys));

            var writeOptions = new XmlSerializerOptions
            {
                ReferenceHandler = new DirectoryReferenceHandler(),
                OmitXmlDeclaration = true,
            };

            output.WriteLine(XmlSerializer.Serialize(project, writeOptions));
        }

        /// <summary>
        /// Shows what happens when a graph with a cycle is serialized without a reference handler.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteCycleWithoutHandler(TextWriter output)
        {
            var tyler = new Employee { Name = "Tyler Stein" };
            var adrian = new Employee { Name = "Adrian King", Manager = tyler };
            tyler.DirectReports.Add(adrian);

            try
            {
                XmlSerializer.Serialize(tyler);
            }
            catch (Exception ex)
            {
                output.WriteLine(ex.GetType().Name);
                output.WriteLine(ex.Message);
            }
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// A reference handler that keeps every identified object in a dictionary the caller can read after the
        /// document has been deserialized.
        /// </summary>
        private sealed class DirectoryReferenceHandler : XmlReferenceHandler
        {

            #region Public Properties

            /// <summary>
            /// Gets the objects that were read, keyed by the identifier the document gave them.
            /// </summary>
            public Dictionary<string, object> Directory { get; } = new(StringComparer.Ordinal);

            #endregion

            #region Public Methods

            /// <inheritdoc />
            public override XmlReferenceResolver CreateResolver()
            {
                return new DirectoryReferenceResolver(Directory);
            }

            #endregion

        }

        /// <summary>
        /// A resolver that stores and finds objects in a dictionary owned by its handler.
        /// </summary>
        /// <param name="directory">The dictionary that maps identifiers to objects.</param>
        private sealed class DirectoryReferenceResolver(Dictionary<string, object> directory) : XmlReferenceResolver
        {

            #region Public Methods

            /// <inheritdoc />
            public override void AddReference(string referenceId, object value)
            {
                directory[referenceId] = value;
            }

            /// <inheritdoc />
            public override string GetReference(object value, out bool alreadyExists)
            {
                foreach (KeyValuePair<string, object> pair in directory)
                {
                    if (ReferenceEquals(pair.Value, value))
                    {
                        alreadyExists = true;
                        return pair.Key;
                    }
                }

                alreadyExists = false;
                string id = $"object-{directory.Count + 1}";
                directory[id] = value;
                return id;
            }

            /// <inheritdoc />
            public override object? ResolveReference(string referenceId)
            {
                return directory.TryGetValue(referenceId, out object? value) ? value : null;
            }

            #endregion

        }

        #endregion

    }

}
