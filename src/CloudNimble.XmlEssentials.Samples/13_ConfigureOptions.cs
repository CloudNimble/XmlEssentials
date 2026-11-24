using System;
using System.IO;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Samples.Models.Serialization;

namespace CloudNimble.XmlEssentials.Samples
{

    /// <summary>
    /// Sample 13: creates, reuses, copies, and freezes <see cref="XmlSerializerOptions"/>.
    /// </summary>
    public static class ConfigureOptions
    {

        #region Fields

        private static readonly XmlSerializerOptions s_compact = new()
        {
            OmitXmlDeclaration = true,
            PropertyNamingPolicy = XmlNamingPolicy.CamelCase,
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Serializes two objects with one options instance that is created once and reused.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Run(TextWriter output)
        {
            output.WriteLine(XmlSerializer.Serialize(new InvoiceLine { Sku = "KB-101", Quantity = 2 }, s_compact));
            output.WriteLine(XmlSerializer.Serialize(new InvoiceCustomer { Name = "Contoso", Email = "ap@contoso.example" }, s_compact));
        }

        /// <summary>
        /// Serializes one object with each built-in options instance.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void BuiltInInstances(TextWriter output)
        {
            var line = new InvoiceLine { Sku = "KB-101", Quantity = 2 };

            output.WriteLine(XmlSerializer.Serialize(line, XmlSerializerOptions.Default));
            output.WriteLine(XmlSerializer.Serialize(line, XmlSerializerOptions.Web));
            output.WriteLine(XmlSerializer.Serialize(line, XmlSerializerOptions.Strict));
        }

        /// <summary>
        /// Deserializes XML with a duplicate element and a comment under the general defaults and under the strict defaults.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void StrictDefaults(TextWriter output)
        {
            string[] documents =
            [
                "<InvoiceLine><Sku>KB-101</Sku><Sku>MS-220</Sku></InvoiceLine>",
                "<InvoiceLine><!-- checked --><Sku>KB-101</Sku></InvoiceLine>",
                "<InvoiceLine><Sku>KB-101</Sku><Colour>black</Colour></InvoiceLine>",
            ];

            foreach (string xml in documents)
            {
                InvoiceLine? line = XmlSerializer.Deserialize<InvoiceLine>(xml);
                output.WriteLine($"General: {line!.Sku}");

                try
                {
                    XmlSerializer.Deserialize<InvoiceLine>(xml, XmlSerializerOptions.Strict);
                }
                catch (XmlException ex)
                {
                    output.WriteLine($"Strict: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Starts from a set of defaults, and copies an existing instance to change one setting.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void CopyAndChange(TextWriter output)
        {
            var web = new XmlSerializerOptions(XmlSerializerDefaults.Web)
            {
                WriteIndented = true,
                NewLine = "\n",
            };

            var line = new InvoiceLine { Sku = "KB-101", Quantity = 2 };
            output.WriteLine(XmlSerializer.Serialize(line, web));

            var flat = new XmlSerializerOptions(web) { WriteIndented = false };
            output.WriteLine($"Source is read-only: {web.IsReadOnly}. Copy is read-only: {flat.IsReadOnly}.");
            output.WriteLine(XmlSerializer.Serialize(line, flat));
        }

        /// <summary>
        /// Shows when an options instance becomes read-only and what happens when it is changed afterwards.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void ReadOnly(TextWriter output)
        {
            var options = new XmlSerializerOptions { OmitXmlDeclaration = true };
            output.WriteLine($"Before first use: {options.IsReadOnly}");

            XmlSerializer.Serialize(new InvoiceLine { Sku = "KB-101", Quantity = 2 }, options);
            output.WriteLine($"After first use: {options.IsReadOnly}");

            try
            {
                options.WriteIndented = true;
            }
            catch (InvalidOperationException ex)
            {
                output.WriteLine(ex.Message);
            }

            var frozen = new XmlSerializerOptions();
            frozen.MakeReadOnly();
            output.WriteLine($"After MakeReadOnly: {frozen.IsReadOnly}");
        }

        /// <summary>
        /// Prints the value every option has on a new <see cref="XmlSerializerOptions"/> instance.
        /// </summary>
        /// <param name="output">The writer that receives what the sample prints.</param>
        public static void Defaults(TextWriter output)
        {
            var options = new XmlSerializerOptions();

            output.WriteLine($"AllowDuplicateProperties = {options.AllowDuplicateProperties}");
            output.WriteLine($"AttributeNamingPolicy = {options.AttributeNamingPolicy?.ToString() ?? "null"}");
            output.WriteLine($"AttributeStyle = {options.AttributeStyle}");
            output.WriteLine($"AttributesPerLine = {options.AttributesPerLine}");
            output.WriteLine($"Converters.Count = {options.Converters.Count}");
            output.WriteLine($"DefaultBufferSize = {options.DefaultBufferSize}");
            output.WriteLine($"DefaultIgnoreCondition = {options.DefaultIgnoreCondition}");
            output.WriteLine($"DefaultNamespace = {options.DefaultNamespace ?? "null"}");
            output.WriteLine($"DefaultPropertyMapping = {options.DefaultPropertyMapping}");
            output.WriteLine($"DictionaryKeyPolicy = {options.DictionaryKeyPolicy?.ToString() ?? "null"}");
            output.WriteLine($"ElementNamingPolicy = {options.ElementNamingPolicy?.ToString() ?? "null"}");
            output.WriteLine($"EmitByteOrderMark = {options.EmitByteOrderMark}");
            output.WriteLine($"EmptyElementStyle = {options.EmptyElementStyle}");
            output.WriteLine($"EnumHandling = {options.EnumHandling}");
            output.WriteLine($"IgnoreReadOnlyFields = {options.IgnoreReadOnlyFields}");
            output.WriteLine($"IgnoreReadOnlyProperties = {options.IgnoreReadOnlyProperties}");
            output.WriteLine($"IncludeFields = {options.IncludeFields}");
            output.WriteLine($"IndentCharacter = '{options.IndentCharacter}'");
            output.WriteLine($"IndentSize = {options.IndentSize}");
            output.WriteLine($"InferClosedTypePolymorphism = {options.InferClosedTypePolymorphism}");
            output.WriteLine($"IsReadOnly = {options.IsReadOnly}");
            output.WriteLine($"MaxAttributeCount = {options.MaxAttributeCount}");
            output.WriteLine($"MaxCommentByteCount = {options.MaxCommentByteCount}");
            output.WriteLine($"MaxDepth = {options.MaxDepth}");
            output.WriteLine($"MaxDocumentByteCount = {options.MaxDocumentByteCount}");
            output.WriteLine($"MaxNameByteCount = {options.MaxNameByteCount}");
            output.WriteLine($"MaxTextByteCount = {options.MaxTextByteCount}");
            output.WriteLine($"NewLine is Environment.NewLine = {options.NewLine == Environment.NewLine}");
            output.WriteLine($"NumberHandling = {options.NumberHandling}");
            output.WriteLine($"OmitXmlDeclaration = {options.OmitXmlDeclaration}");
            output.WriteLine($"PreferredObjectCreationHandling = {options.PreferredObjectCreationHandling}");
            output.WriteLine($"PropertyNameCaseInsensitive = {options.PropertyNameCaseInsensitive}");
            output.WriteLine($"PropertyNamingPolicy = {options.PropertyNamingPolicy?.ToString() ?? "null"}");
            output.WriteLine($"ReadCommentHandling = {options.ReadCommentHandling}");
            output.WriteLine($"ReferenceHandler = {options.ReferenceHandler?.ToString() ?? "null"}");
            output.WriteLine($"RespectNullableAnnotations = {options.RespectNullableAnnotations}");
            output.WriteLine($"RespectRequiredConstructorParameters = {options.RespectRequiredConstructorParameters}");
            output.WriteLine($"TypeInfoResolverChain.Count = {options.TypeInfoResolverChain.Count}");
            output.WriteLine($"UnknownTypeHandling = {options.UnknownTypeHandling}");
            output.WriteLine($"UnmappedMemberHandling = {options.UnmappedMemberHandling}");
            output.WriteLine($"WhitespaceHandling = {options.WhitespaceHandling}");
            output.WriteLine($"WriteIndented = {options.WriteIndented}");
            output.WriteLine($"XmlVersion = {options.XmlVersion}");
        }

        #endregion

    }

}
