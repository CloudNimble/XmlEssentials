using System;
using System.IO;
using CloudNimble.Text.Xml;
using McMaster.Extensions.CommandLineUtils;

namespace CloudNimble.XmlEssentials.Samples.Cli
{

    /// <summary>
    /// Walks a UTF-8 XML file with <see cref="Utf8XmlReader"/> and prints token counts.
    /// </summary>
    public static class ParseCommand
    {

        #region Public Methods

        /// <summary>
        /// Adds the <c>parse</c> command to <paramref name="app"/>.
        /// </summary>
        /// <param name="app">The application.</param>
        internal static void Configure(CommandLineApplication app)
        {
            app.Command("parse", parse =>
            {
                parse.Description = "Parse a UTF-8 XML file and print start-element, attribute, and text counts.";
                parse.HelpOption();
                var file = parse.Argument("file", "Path to a UTF-8 XML file.").IsRequired();
                parse.OnExecute(() => Execute(file.Value));
            });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Parses <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>0 on success, 1 on failure.</returns>
        internal static int Execute(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.Error.WriteLine("parse requires a file path.");
                return 1;
            }

            byte[] utf8;
            try
            {
                utf8 = File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            var reader = new Utf8XmlReader(utf8);
            var starts = 0;
            var attributes = 0;
            var texts = 0;
            try
            {
                while (reader.Read())
                {
                    switch (reader.TokenType)
                    {
                        case XmlTokenType.StartElement:
                            starts++;
                            attributes += reader.AttributeCount;
                            break;
                        case XmlTokenType.Text:
                        case XmlTokenType.CData:
                            texts++;
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (XmlException ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }

            Console.WriteLine("file=" + filePath);
            Console.WriteLine("start-elements=" + starts);
            Console.WriteLine("attributes=" + attributes);
            Console.WriteLine("text-nodes=" + texts);
            return 0;
        }

        #endregion

    }

}
