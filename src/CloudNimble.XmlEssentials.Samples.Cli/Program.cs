using System;
using McMaster.Extensions.CommandLineUtils;

namespace CloudNimble.XmlEssentials.Samples.Cli
{

    /// <summary>
    /// Command-line XML parse and serialize sample for XmlEssentials.
    /// </summary>
    /// <remarks>
    /// Uses the McMaster builder API so Native AOT does not need reflection. Publish with
    /// <c>dotnet publish -c Release</c> (<c>PublishAot</c> is set on the project).
    /// </remarks>
    /// <example>
    /// <code>
    /// xmlessentials serialize --name Ada --age 36
    /// xmlessentials parse catalog.xml
    /// xmlessentials roundtrip person.xml
    /// </code>
    /// </example>
    public static class Program
    {

        #region Public Methods

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>The exit code.</returns>
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "xmlessentials",
                Description = "Parse and serialize XML with XmlEssentials.",
            };
            app.HelpOption();
            ParseCommand.Configure(app);
            RoundtripCommand.Configure(app);
            SerializeCommand.Configure(app);
            app.OnExecute(() =>
            {
                app.ShowHelp();
                return 1;
            });
            return app.Execute(args);
        }

        #endregion

    }

}
