using System.Collections.Generic;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Nodes;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Samples.Models.Shapes;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 18: maps text mixed with elements, CDATA sections, and content that no member maps.
    /// </summary>
    public static class MixedContent
    {

        #region Public Methods

        /// <summary>
        /// Deserializes a paragraph of text and inline elements, changes it, and serializes it again.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            string xml = "<Paragraph>Press <Key>Ctrl</Key> and <Key>S</Key> to save.</Paragraph>";

            Paragraph? paragraph = XmlSerializer.Deserialize<Paragraph>(xml);
            foreach (XmlMixedContentItem item in paragraph!.Content)
            {
                switch (item)
                {
                    case XmlMixedText text:
                        output.WriteLine($"Text: \"{text.Text}\"");
                        break;
                    case XmlMixedElement { Element: { } element }:
                        output.WriteLine($"Element: {element.LocalName} = {element.InnerText}");
                        break;
                }
            }

            var note = new XmlElementNode("Note");
            note.Add(new XmlTextNode("Changes are saved to disk."));
            paragraph.Content.Add(new XmlMixedText(" "));
            paragraph.Content.Add(new XmlMixedElement(note));

            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            output.WriteLine(XmlSerializer.Serialize(paragraph, options));
        }

        /// <summary>
        /// Keeps the attributes and elements that a type does not map, and writes them back.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void KeepUnmappedContent(TextWriter output)
        {
            string xml = """
                <Settings version="2" xmlns:ui="urn:example:ui" ui:density="compact">
                  <Theme>Dark</Theme>
                  <Telemetry enabled="false" />
                  <ui:Font size="14">Cascadia Code</ui:Font>
                </Settings>
                """;

            Settings? settings = XmlSerializer.Deserialize<Settings>(xml);
            output.WriteLine($"Theme: {settings!.Theme}");

            foreach (KeyValuePair<XmlName, string> attribute in settings.Extra!.Attributes)
            {
                output.WriteLine($"Attribute: {attribute.Key} = {attribute.Value}");
            }

            foreach (XmlElementNode element in settings.Extra.Elements)
            {
                output.WriteLine($"Element: {element.LocalName}");
            }

            settings.Theme = "Light";
            var options = new XmlSerializerOptions { WriteIndented = true, NewLine = "\n", OmitXmlDeclaration = true };
            output.WriteLine(XmlSerializer.Serialize(settings, options));
        }

        /// <summary>
        /// Shows what the serializer does with unmapped content when a type has no extension data member.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RejectUnmappedContent(TextWriter output)
        {
            string xml = "<Contact><Name>Ada</Name><Phone>555-0100</Phone></Contact>";

            Contact? contact = XmlSerializer.Deserialize<Contact>(xml);
            output.WriteLine(contact!.Name);

            var strict = new XmlSerializerOptions { UnmappedMemberHandling = XmlUnmappedMemberHandling.Disallow };
            try
            {
                XmlSerializer.Deserialize<Contact>(xml, strict);
            }
            catch (XmlException ex)
            {
                output.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Writes a member as a CDATA section and reads it back.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void WriteCData(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            var script = new Script { Language = "js", Code = "if (a < b && b > 0) { run(); }" };

            string xml = XmlSerializer.Serialize(script, options);
            output.WriteLine(xml);

            Script? copy = XmlSerializer.Deserialize<Script>(xml, options);
            output.WriteLine(copy!.Code);
        }

        #endregion

    }

}
