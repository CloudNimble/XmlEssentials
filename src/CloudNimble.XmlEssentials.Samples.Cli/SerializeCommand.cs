using System;
using CloudNimble.Text.Xml;
using McMaster.Extensions.CommandLineUtils;

namespace CloudNimble.XmlEssentials.Samples.Cli
{

    /// <summary>
    /// Serializes a <see cref="Person"/> through the source-generated context.
    /// </summary>
    public static class SerializeCommand
    {

        #region Public Methods

        /// <summary>
        /// Adds the <c>serialize</c> command to <paramref name="app"/>.
        /// </summary>
        /// <param name="app">The application.</param>
        internal static void Configure(CommandLineApplication app)
        {
            app.Command("serialize", serialize =>
            {
                serialize.Description = "Serialize a Person to XML using the generated XmlSerializerContext.";
                serialize.HelpOption();
                var name = serialize.Option("--name <NAME>", "Given name. Defaults to Ada.", CommandOptionType.SingleValue);
                name.DefaultValue = "Ada";
                var age = serialize.Option("--age <AGE>", "Age in years. Defaults to 36.", CommandOptionType.SingleValue);
                age.DefaultValue = "36";
                serialize.OnExecute(() => Execute(name.Value(), age.Value()));
            });
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Writes the XML document to standard output.
        /// </summary>
        /// <param name="name">The given name.</param>
        /// <param name="ageText">The age as text.</param>
        /// <returns>0 on success, 1 when the age is not an integer.</returns>
        internal static int Execute(string? name, string? ageText)
        {
            if (!int.TryParse(ageText, out var age))
            {
                Console.Error.WriteLine("age must be an integer.");
                return 1;
            }

            var xml = XmlSerializer.Serialize(new Person { Name = name, Age = age }, AppXmlContext.Default.Person);
            Console.WriteLine(xml);
            return 0;
        }

        #endregion

    }

}
