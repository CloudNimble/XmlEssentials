using System;
using System.IO;
using CloudNimble.Text.Xml;
using McMaster.Extensions.CommandLineUtils;

namespace CloudNimble.XmlEssentials.Samples.Cli
{

    /// <summary>
    /// Reads a Person document and writes it back through the source-generated context.
    /// </summary>
    public static class RoundtripCommand
    {

        #region Public Methods

        /// <summary>
        /// Adds the <c>roundtrip</c> command to <paramref name="app"/>.
        /// </summary>
        /// <param name="app">The application.</param>
        internal static void Configure(CommandLineApplication app)
        {
            app.Command("roundtrip", roundtrip =>
            {
                roundtrip.Description = "Deserialize a Person from a file and serialize it back to standard output.";
                roundtrip.HelpOption();
                var file = roundtrip.Argument("file", "Path to a Person XML file.").IsRequired();
                roundtrip.OnExecute(() => Execute(file.Value));
            });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Round-trips the document at <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>0 on success, 1 on failure.</returns>
        internal static int Execute(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.Error.WriteLine("roundtrip requires a file path.");
                return 1;
            }

            string xml;
            try
            {
                xml = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            Person? person;
            try
            {
                person = XmlSerializer.Deserialize(xml, AppXmlContext.Default.Person);
            }
            catch (XmlException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            if (person is null)
            {
                Console.Error.WriteLine("The document deserialized as null.");
                return 1;
            }

            Console.WriteLine(XmlSerializer.Serialize(person, AppXmlContext.Default.Person));
            return 0;
        }

        #endregion

    }

}
