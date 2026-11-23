using System;
using CloudNimble.Text.Xml;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies compile-time source generator configuration when applied to <see cref="XmlSerializerContext"/> class declarations.
    /// This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The values are the default options of the generated context. They apply when the context is created without options.
    /// When the caller passes an <see cref="XmlSerializerOptions"/> instance to the constructor, as in
    /// <c>new AppXmlContext(options)</c>, that instance is used as it is and takes precedence over this attribute.
    /// </para>
    /// <para>
    /// The source generator reads <see cref="AttributeNamingPolicy"/>, <see cref="DefaultIgnoreCondition"/>,
    /// <see cref="DefaultNamespace"/>, <see cref="DefaultPropertyMapping"/>, <see cref="ElementNamingPolicy"/>,
    /// <see cref="GenerationMode"/>, <see cref="IncludeFields"/>, <see cref="NilHandling"/>,
    /// <see cref="OmitXmlDeclaration"/>, <see cref="PropertyNamingPolicy"/>, and <see cref="WriteIndented"/>.
    /// It does not read the other properties.
    /// </para>
    /// <para>
    /// This attribute is analogous to <c>JsonSourceGenerationOptionsAttribute</c> in <c>System.Text.Json</c>.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class XmlSourceGenerationOptionsAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the built-in naming policy that converts member names to XML attribute names.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the naming policy for attributes. The default is <see cref="XmlKnownNamingPolicy.Unspecified"/>.
        /// </value>
        public XmlKnownNamingPolicy AttributeNamingPolicy { get; set; }

        /// <summary>
        /// Gets or sets the default mapping of a member that has no mapping attribute: a child element or an XML attribute.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the default mapping of members. The default is <see cref="XmlPropertyMapping.Element"/>.
        /// </value>
        public XmlPropertyMapping DefaultPropertyMapping { get; set; }

        /// <summary>
        /// Gets or sets the default condition that determines when members are ignored.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies when members are ignored. The default is <see cref="XmlIgnoreCondition.Never"/>.
        /// </value>
        public XmlIgnoreCondition DefaultIgnoreCondition { get; set; }

        /// <summary>
        /// Gets or sets the default namespace URI of the document element.
        /// </summary>
        /// <value>
        /// The default namespace URI, or <see langword="null"/> for no namespace. The default is <see langword="null"/>.
        /// </value>
        public string? DefaultNamespace { get; set; }

        /// <summary>
        /// Gets or sets where the type discriminator of a polymorphic type is written.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies where the type discriminator is written. The default is
        /// <see cref="XmlDiscriminatorPlacement.ElementName"/>.
        /// </value>
        public XmlDiscriminatorPlacement DiscriminatorPlacement { get; set; }

        /// <summary>
        /// Gets or sets the built-in naming policy that converts type and member names to XML element names.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the naming policy for elements. The default is <see cref="XmlKnownNamingPolicy.Unspecified"/>.
        /// </value>
        public XmlKnownNamingPolicy ElementNamingPolicy { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether a UTF-8 byte order mark is written.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if a UTF-8 byte order mark is written; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool EmitByteOrderMark { get; set; }

        /// <summary>
        /// Gets or sets how enumeration values are written.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies how enumeration values are written. The default is <see cref="XmlEnumHandling.Name"/>.
        /// </value>
        public XmlEnumHandling EnumHandling { get; set; }

        /// <summary>
        /// Gets or sets the source generation mode for the types of the context.
        /// </summary>
        /// <value>
        /// A bitwise combination of the enumeration values that specifies what the source generator generates. The default is
        /// <see cref="XmlSourceGenerationMode.Default"/>, which generates both type metadata and serialization logic.
        /// </value>
        public XmlSourceGenerationMode GenerationMode { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether fields are serialized and deserialized.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if public fields are serialized and deserialized; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool IncludeFields { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether read-only fields are ignored.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if read-only fields are ignored; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool IgnoreReadOnlyFields { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether read-only properties are ignored.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if read-only properties are ignored; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool IgnoreReadOnlyProperties { get; set; }

        /// <summary>
        /// Gets or sets the maximum depth of nested elements.
        /// </summary>
        /// <value>
        /// The maximum element depth. A value of 0 selects the default limit. The default is 0.
        /// </value>
        public int MaxDepth { get; set; }

        /// <summary>
        /// Gets or sets how <see langword="null"/> values are written.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies how <see langword="null"/> values are written. The default is <see cref="XmlNilHandling.WriteXsiNil"/>.
        /// </value>
        public XmlNilHandling NilHandling { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the XML declaration is omitted.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the XML declaration is omitted; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool OmitXmlDeclaration { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether element and attribute names are matched to members without regard to case on deserialization.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if names are compared without regard to case; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool PropertyNameCaseInsensitive { get; set; }

        /// <summary>
        /// Gets or sets the built-in naming policy that converts names to XML names when <see cref="ElementNamingPolicy"/>
        /// or <see cref="AttributeNamingPolicy"/> is not set.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the naming policy for members. The default is <see cref="XmlKnownNamingPolicy.Unspecified"/>.
        /// </value>
        public XmlKnownNamingPolicy PropertyNamingPolicy { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether nullable annotations are respected.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if nullable annotations are respected; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool RespectNullableAnnotations { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether enumeration values are written as names even when
        /// <see cref="EnumHandling"/> is <see cref="XmlEnumHandling.Integer"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if enumeration values are always written as names; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool UseStringEnumConverter { get; set; }

        /// <summary>
        /// Gets or sets a value that indicates whether the output is indented.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the output is indented; otherwise, <see langword="false"/>. The default is <see langword="false"/>.
        /// </value>
        public bool WriteIndented { get; set; }

        /// <summary>
        /// Gets or sets the XML version that is written in the XML declaration.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies the XML version. The default is <see cref="CloudNimble.Text.Xml.XmlVersion.Version10"/>.
        /// </value>
        public XmlVersion XmlVersion { get; set; }

        #endregion

    }

}
