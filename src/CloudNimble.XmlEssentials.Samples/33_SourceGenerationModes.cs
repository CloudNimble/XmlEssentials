using System;
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
        /// Writes and reads the same order with a context generated in each mode.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            XmlTypeInfo<Order> both = OrderXmlContext.Default.Order;
            XmlTypeInfo<Order> metadata = MetadataOrderXmlContext.Default.Order;
            XmlTypeInfo<Order> serialization = SerializationOrderXmlContext.Default.Order;

            var order = new Order { Id = 7, Customer = "Grace" };
            string xml = XmlSerializer.Serialize(order, both);
            output.WriteLine($"Default:       {xml}");
            output.WriteLine($"Metadata:      {XmlSerializer.Serialize(order, metadata)}");
            output.WriteLine($"Serialization: {XmlSerializer.Serialize(order, serialization)}");

            output.WriteLine($"Default:       {both.Properties.Count} properties, reads {XmlSerializer.Deserialize(xml, both)!.Customer}");
            output.WriteLine($"Metadata:      {metadata.Properties.Count} properties, reads {XmlSerializer.Deserialize(xml, metadata)!.Customer}");
            try
            {
                XmlSerializer.Deserialize(xml, serialization);
            }
            catch (InvalidOperationException ex)
            {
                output.WriteLine($"Serialization: {serialization.Properties.Count} properties, {ex.Message}");
            }
        }

        /// <summary>
        /// Lists the members that the generated metadata describes.
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
