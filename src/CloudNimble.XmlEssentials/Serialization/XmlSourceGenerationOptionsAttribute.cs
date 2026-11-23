using System;
using CloudNimble.Text.Xml;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Default options for a source-generated <see cref="XmlSerializerContext"/>. Analogous to
    /// <c>JsonSourceGenerationOptionsAttribute</c>. Constructor <see cref="XmlSerializerOptions"/> win when
    /// the consumer passes <c>new AppXmlContext(options)</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class XmlSourceGenerationOptionsAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the naming policy for attributes.
        /// </summary>
        public XmlKnownNamingPolicy AttributeNamingPolicy { get; set; }

        /// <summary>
        /// Gets or sets the default property mapping (element or attribute).
        /// </summary>
        public XmlPropertyMapping DefaultPropertyMapping { get; set; }

        /// <summary>
        /// Gets or sets the default ignore condition.
        /// </summary>
        public XmlIgnoreCondition DefaultIgnoreCondition { get; set; }

        /// <summary>
        /// Gets or sets the default namespace URI of serialized documents.
        /// </summary>
        public string? DefaultNamespace { get; set; }

        /// <summary>
        /// Gets or sets where a polymorphic discriminator is written.
        /// </summary>
        public XmlDiscriminatorPlacement DiscriminatorPlacement { get; set; }

        /// <summary>
        /// Gets or sets the naming policy for elements.
        /// </summary>
        public XmlKnownNamingPolicy ElementNamingPolicy { get; set; }

        /// <summary>
        /// Gets or sets whether a UTF-8 BOM is written.
        /// </summary>
        public bool EmitByteOrderMark { get; set; }

        /// <summary>
        /// Gets or sets how enumerations are written.
        /// </summary>
        public XmlEnumHandling EnumHandling { get; set; }

        /// <summary>
        /// Gets or sets the generation mode. <see cref="XmlSourceGenerationMode.Default"/> emits metadata and serialization.
        /// </summary>
        public XmlSourceGenerationMode GenerationMode { get; set; }

        /// <summary>
        /// Gets or sets whether public fields are serialized.
        /// </summary>
        public bool IncludeFields { get; set; }

        /// <summary>
        /// Gets or sets whether read-only fields are omitted.
        /// </summary>
        public bool IgnoreReadOnlyFields { get; set; }

        /// <summary>
        /// Gets or sets whether read-only properties are omitted.
        /// </summary>
        public bool IgnoreReadOnlyProperties { get; set; }

        /// <summary>
        /// Gets or sets the maximum object graph depth.
        /// </summary>
        public int MaxDepth { get; set; }

        /// <summary>
        /// Gets or sets how null values are written.
        /// </summary>
        public XmlNilHandling NilHandling { get; set; }

        /// <summary>
        /// Gets or sets whether the XML declaration is omitted.
        /// </summary>
        public bool OmitXmlDeclaration { get; set; }

        /// <summary>
        /// Gets or sets whether property names are compared without regard to case.
        /// </summary>
        public bool PropertyNameCaseInsensitive { get; set; }

        /// <summary>
        /// Gets or sets the naming policy for properties.
        /// </summary>
        public XmlKnownNamingPolicy PropertyNamingPolicy { get; set; }

        /// <summary>
        /// Gets or sets whether nullable annotations are respected.
        /// </summary>
        public bool RespectNullableAnnotations { get; set; }

        /// <summary>
        /// Gets or sets whether enums are written as names even when <see cref="EnumHandling"/> is integer.
        /// </summary>
        public bool UseStringEnumConverter { get; set; }

        /// <summary>
        /// Gets or sets whether output is indented.
        /// </summary>
        public bool WriteIndented { get; set; }

        /// <summary>
        /// Gets or sets the XML version written in the declaration.
        /// </summary>
        public XmlVersion XmlVersion { get; set; }

        #endregion

    }

}
