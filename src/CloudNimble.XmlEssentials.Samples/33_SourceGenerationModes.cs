using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Samples.Models.Advanced;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 33: what a context holds in each <see cref="XmlSourceGenerationMode"/>.
    /// </summary>
    public static class SourceGenerationModes
    {

        #region Public Methods

        /// <summary>
        /// Compares the type info of a context generated in the default mode with one generated in metadata mode.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            XmlTypeInfo<Order> generated = OrderXmlContext.Default.Order;
            XmlTypeInfo<Order> metadata = MetadataOrderXmlContext.Default.Order;

            output.WriteLine($"Default:  {generated.Converter.GetType().Name}, {generated.Properties.Count} properties");
            output.WriteLine($"Metadata: {metadata.Converter.GetType().Name}, {metadata.Properties.Count} properties");

            var order = new Order { Id = 7, Customer = "Grace" };
            output.WriteLine(XmlSerializer.Serialize(order, generated));
            output.WriteLine(XmlSerializer.Serialize(order, metadata));
        }

        /// <summary>
        /// Lists the members that metadata mode describes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void InspectMetadata(TextWriter output)
        {
            XmlTypeInfo<Order> metadata = MetadataOrderXmlContext.Default.Order;

            output.WriteLine($"Root element: {metadata.RootName.LocalName}");
            foreach (XmlPropertyInfo property in metadata.Properties)
            {
                string kind = property.IsAttribute ? "attribute" : "element";
                output.WriteLine($"{property.Name.LocalName}: {kind}, {property.PropertyType.Type.Name}");
            }
        }

        #endregion

    }

}
