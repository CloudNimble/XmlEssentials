using System;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Serialization;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 8: controls the names, the shape, and the order of the XML that <see cref="XmlSerializer"/> writes.
    /// </summary>
    public static class CustomizeNames
    {

        #region Public Methods

        /// <summary>
        /// Serializes a <see cref="CatalogItem"/>, whose attributes name the root element, an XML attribute, and two child elements.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            var item = new CatalogItem { Sku = "KB-101", Name = "Keyboard", UnitPrice = 49.95m };
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(item, options);
            output.WriteLine(xml);

            CatalogItem? copy = XmlSerializer.Deserialize<CatalogItem>(xml, options);
            output.WriteLine($"{copy!.Sku}: {copy.Name}");
        }

        /// <summary>
        /// Serializes a <see cref="Listing"/>, whose <see cref="Price"/> is written as an attribute and element text.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void TextContent(TextWriter output)
        {
            var listing = new Listing
            {
                Name = "Keyboard",
                Price = new Price { Currency = "USD", Amount = 49.95m },
            };

            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(listing, options);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Serializes an <see cref="Address"/>, whose elements are ordered by attribute.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ElementOrder(TextWriter output)
        {
            var address = new Address { PostalCode = "98052", City = "Redmond", Street = "1 Main St" };
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };

            string xml = XmlSerializer.Serialize(address, options);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Serializes a <see cref="Warehouse"/> with one naming policy for every name.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void NamingPolicy(TextWriter output)
        {
            var warehouse = new Warehouse { WarehouseCode = "SEA-2", StreetAddress = "400 Dock Rd", LoadingDocks = 12 };

            var options = new XmlSerializerOptions
            {
                OmitXmlDeclaration = true,
                PropertyNamingPolicy = XmlNamingPolicy.CamelCase,
            };

            string xml = XmlSerializer.Serialize(warehouse, options);
            output.WriteLine(xml);

            Warehouse? copy = XmlSerializer.Deserialize<Warehouse>(xml, options);
            output.WriteLine($"{copy!.WarehouseCode}: {copy.LoadingDocks} docks");
        }

        /// <summary>
        /// Serializes a <see cref="Warehouse"/> with one naming policy for elements and another for attributes.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ElementAndAttributePolicies(TextWriter output)
        {
            var warehouse = new Warehouse { WarehouseCode = "SEA-2", StreetAddress = "400 Dock Rd", LoadingDocks = 12 };

            var options = new XmlSerializerOptions
            {
                OmitXmlDeclaration = true,
                ElementNamingPolicy = XmlNamingPolicy.KebabCaseLower,
                AttributeNamingPolicy = XmlNamingPolicy.SnakeCaseLower,
            };

            string xml = XmlSerializer.Serialize(warehouse, options);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Converts one name with each built-in naming policy.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void BuiltInPolicies(TextWriter output)
        {
            const string name = "StreetAddress";

            output.WriteLine($"CamelCase:      {XmlNamingPolicy.CamelCase.ConvertName(name)}");
            output.WriteLine($"PascalCase:     {XmlNamingPolicy.PascalCase.ConvertName("streetAddress")}");
            output.WriteLine($"SnakeCaseLower: {XmlNamingPolicy.SnakeCaseLower.ConvertName(name)}");
            output.WriteLine($"SnakeCaseUpper: {XmlNamingPolicy.SnakeCaseUpper.ConvertName(name)}");
            output.WriteLine($"KebabCaseLower: {XmlNamingPolicy.KebabCaseLower.ConvertName(name)}");
            output.WriteLine($"KebabCaseUpper: {XmlNamingPolicy.KebabCaseUpper.ConvertName(name)}");
        }

        /// <summary>
        /// Serializes a <see cref="Warehouse"/> with a naming policy defined by the application.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void CustomPolicy(TextWriter output)
        {
            var warehouse = new Warehouse { WarehouseCode = "SEA-2", StreetAddress = "400 Dock Rd", LoadingDocks = 12 };

            var options = new XmlSerializerOptions
            {
                OmitXmlDeclaration = true,
                PropertyNamingPolicy = new LowerCaseNamingPolicy(),
            };

            string xml = XmlSerializer.Serialize(warehouse, options);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Serializes a <see cref="PurchaseOrder"/> with its simple values as XML attributes, without annotating the type.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void AttributesByDefault(TextWriter output)
        {
            var order = new PurchaseOrder
            {
                Id = 1042,
                Customer = "Contoso",
                Placed = new DateOnly(2026, 3, 14),
                Total = 129.90m,
            };

            var options = new XmlSerializerOptions
            {
                OmitXmlDeclaration = true,
                DefaultPropertyMapping = XmlPropertyMapping.Attribute,
            };

            string xml = XmlSerializer.Serialize(order, options);
            output.WriteLine(xml);
        }

        /// <summary>
        /// Serializes an enumeration value by name and as a number, and reads both forms.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void EnumValues(TextWriter output)
        {
            var parcel = new Parcel { TrackingNumber = "1Z999", Status = ParcelStatus.InTransit };

            var byName = new XmlSerializerOptions { OmitXmlDeclaration = true, PropertyNamingPolicy = XmlNamingPolicy.CamelCase };
            output.WriteLine(XmlSerializer.Serialize(parcel, byName));

            var byNumber = new XmlSerializerOptions { OmitXmlDeclaration = true, EnumHandling = XmlEnumHandling.Integer };
            output.WriteLine(XmlSerializer.Serialize(parcel, byNumber));

            Parcel? fromName = XmlSerializer.Deserialize<Parcel>("<Parcel><Status>Delivered</Status></Parcel>");
            Parcel? fromNumber = XmlSerializer.Deserialize<Parcel>("<Parcel><Status>2</Status></Parcel>");
            output.WriteLine($"{fromName!.Status}, {fromNumber!.Status}");
        }

        /// <summary>
        /// Gives an enumeration member an XML name of its own, and reads names in any case.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void RenameEnumValues(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            output.WriteLine(XmlSerializer.Serialize(ShippingMethod.NextDay, options));

            ShippingMethod renamed = XmlSerializer.Deserialize<ShippingMethod>("<ShippingMethod>next-day</ShippingMethod>");
            ShippingMethod anyCase = XmlSerializer.Deserialize<ShippingMethod>("<ShippingMethod>STANDARD</ShippingMethod>");
            output.WriteLine($"{renamed}, {anyCase}");
        }

        #endregion

    }

}
