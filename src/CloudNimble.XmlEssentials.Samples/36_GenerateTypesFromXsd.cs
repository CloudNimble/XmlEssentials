using System;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Schemas;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 36: types and a context that the generator creates from <c>Schemas\catalog.xsd</c>.
    /// </summary>
    public static class GenerateTypesFromXsd
    {

        #region Public Methods

        /// <summary>
        /// Builds a catalog from the generated classes and round-trips it through the generated context.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var catalog = new Catalog
            {
                published = new DateOnly(2026, 11, 23),
                Product =
                [
                    new Catalog_Product
                    {
                        sku = "KB-01",
                        Name = "Keyboard",
                        Price = 89.90m,
                        Availability = Availability.InStock,
                        Weight = 0.8,
                        Supplier = new Supplier { Name = "Contoso", Country = "NL" },
                    },
                    new Catalog_Product
                    {
                        sku = "MS-02",
                        Name = "Mouse",
                        Price = 20m,
                        Availability = Availability.BackOrder,
                        Supplier = new Supplier { Name = "Fabrikam", Country = "DE" },
                    },
                ],
            };

            var options = new XmlSerializerOptions
            {
                DefaultNamespace = "urn:example:catalog",
                NewLine = "\n",
                WriteIndented = true,
            };
            var context = new CatalogXmlContext(options);

            string xml = XmlSerializer.Serialize(catalog, context.Catalog);
            output.WriteLine(xml);

            Catalog? copy = XmlSerializer.Deserialize(xml, context.Catalog);
            foreach (Catalog_Product product in copy!.Product!)
            {
                output.WriteLine($"{product.sku}: {product.Name}, {product.Availability}, from {product.Supplier!.Name}");
            }
        }

        /// <summary>
        /// Serializes with the generated <c>Default</c> context, whose options carry the schema's target namespace.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void UseDefaultContext(TextWriter output)
        {
            var supplier = new Supplier { Name = "Contoso", Country = "NL" };

            output.WriteLine(CatalogXmlContext.Default.Options.DefaultNamespace);
            output.WriteLine(XmlSerializer.Serialize(supplier, CatalogXmlContext.Default.Supplier));
        }

        #endregion

    }

}
