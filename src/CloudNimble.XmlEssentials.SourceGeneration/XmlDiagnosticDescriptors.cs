using Microsoft.CodeAnalysis;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Diagnostics emitted by <see cref="SerializerGenerator"/> and <see cref="XsdGenerator"/>.
    /// </summary>
    internal static class XmlDiagnosticDescriptors
    {

        #region Fields

        /// <summary>
        /// XMLXE001: the annotated type is not a partial class that inherits <c>XmlSerializerContext</c>.
        /// </summary>
        internal static readonly DiagnosticDescriptor ContextMustBePartial = new(
            id: "XMLXE001",
            title: "XmlSerializerContext must be a partial class",
            messageFormat: "'{0}' must be a partial class that inherits XmlSerializerContext",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE002: the type cannot be serialized.
        /// </summary>
        internal static readonly DiagnosticDescriptor UnsupportedType = new(
            id: "XMLXE002",
            title: "Unsupported type",
            messageFormat: "The type '{0}' cannot be serialized by XmlEssentials",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE003: a complex type is mapped as an XML attribute.
        /// </summary>
        internal static readonly DiagnosticDescriptor ComplexTypeAsAttribute = new(
            id: "XMLXE003",
            title: "Complex types cannot be attributes",
            messageFormat: "Member '{0}.{1}' is mapped as an XML attribute",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE004: more than one simple-content member.
        /// </summary>
        internal static readonly DiagnosticDescriptor MultipleTextMembers = new(
            id: "XMLXE004",
            title: "Multiple text members",
            messageFormat: "The type '{0}' has more than one [XmlText] or [XmlCData]",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE005: more than one bindable constructor.
        /// </summary>
        internal static readonly DiagnosticDescriptor AmbiguousConstructors = new(
            id: "XMLXE005",
            title: "Ambiguous constructors",
            messageFormat: "The type '{0}' has more than one constructor that can bind its members",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE006: a mapped name is not a valid XML Name.
        /// </summary>
        internal static readonly DiagnosticDescriptor InvalidXmlName = new(
            id: "XMLXE006",
            title: "Invalid XML name",
            messageFormat: "The XML name '{0}' on '{1}' is not a valid XML Name",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE008: a type or member declares derived types, which generated converters do not read or write.
        /// </summary>
        internal static readonly DiagnosticDescriptor DerivedTypesNotGenerated = new(
            id: "XMLXE008",
            title: "Derived types are not generated",
            messageFormat: "'{0}' declares derived types with [XmlDerivedType]; a generated converter would read and write only the declared type, so use the reflection resolver for it",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE009: a member names its own converter, which generated converters do not call.
        /// </summary>
        internal static readonly DiagnosticDescriptor MemberConverterNotGenerated = new(
            id: "XMLXE009",
            title: "A converter on a member is not generated",
            messageFormat: "'{0}' names a converter with [XmlConverter]; a generated converter would not call it, so put the attribute on the member's type or use the reflection resolver for the declaring type",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE007: BCL XmlInclude or our XmlInclude on a type.
        /// </summary>
        internal static readonly DiagnosticDescriptor UseXmlDerivedType = new(
            id: "XMLXE007",
            title: "Use [XmlDerivedType]",
            messageFormat: "The type '{0}' uses XmlIncludeAttribute; use [XmlDerivedType] for known derived types",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE010: include/import is not an AdditionalFile, or the schema contains a DTD.
        /// </summary>
        internal static readonly DiagnosticDescriptor SchemaResolution = new(
            id: "XMLXE010",
            title: "Schema include or DTD is not allowed",
            messageFormat: "{0}",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE011: unsupported schema construct.
        /// </summary>
        internal static readonly DiagnosticDescriptor UnsupportedSchemaConstruct = new(
            id: "XMLXE011",
            title: "Unsupported schema construct",
            messageFormat: "{0}",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE012: generated C# name collision.
        /// </summary>
        internal static readonly DiagnosticDescriptor GeneratedNameCollision = new(
            id: "XMLXE012",
            title: "Generated name collision",
            messageFormat: "The generated C# name '{0}' is used more than once",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE013: generated member XML name is invalid.
        /// </summary>
        internal static readonly DiagnosticDescriptor GeneratedInvalidXmlName = new(
            id: "XMLXE013",
            title: "Invalid generated XML name",
            messageFormat: "The XML name '{0}' is not a valid XML Name",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE014: a member holds mixed content, which generated converters do not read or write.
        /// </summary>
        internal static readonly DiagnosticDescriptor MixedContentNotGenerated = new(
            id: "XMLXE014",
            title: "Mixed content is not generated",
            messageFormat: "'{0}' is marked [XmlMixed]; a generated converter would not read or write mixed content, so use the reflection resolver for the declaring type",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE015: a complex type is mapped as the text of an element.
        /// </summary>
        internal static readonly DiagnosticDescriptor ComplexTypeAsText = new(
            id: "XMLXE015",
            title: "Complex types cannot be text",
            messageFormat: "Member '{0}.{1}' is mapped as XML text, and its type is not a single value",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE016: a member is marked to be populated and a generated converter cannot populate it.
        /// </summary>
        internal static readonly DiagnosticDescriptor PopulateNotGenerated = new(
            id: "XMLXE016",
            title: "Member cannot be populated",
            messageFormat: "'{0}' is marked with XmlObjectCreationHandling.Populate, {1}",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLXE017: a required member is marked as never read.
        /// </summary>
        internal static readonly DiagnosticDescriptor RequiredMemberNeverRead = new(
            id: "XMLXE017",
            title: "Required member is never read",
            messageFormat: "'{0}' is required, and its ignore condition is XmlIgnoreCondition.WhenReading, so it would never be read",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        #endregion

    }

}
