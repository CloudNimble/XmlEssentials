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
        /// XMLESS001: the annotated type is not a partial class that inherits <c>XmlSerializerContext</c>.
        /// </summary>
        internal static readonly DiagnosticDescriptor ContextMustBePartial = new(
            id: "XMLESS001",
            title: "XmlSerializerContext must be a partial class",
            messageFormat: "'{0}' must be a partial class that inherits XmlSerializerContext",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS002: the type cannot be serialized.
        /// </summary>
        internal static readonly DiagnosticDescriptor UnsupportedType = new(
            id: "XMLESS002",
            title: "Unsupported type",
            messageFormat: "The type '{0}' cannot be serialized by XmlEssentials",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS003: a complex type is mapped as an XML attribute.
        /// </summary>
        internal static readonly DiagnosticDescriptor ComplexTypeAsAttribute = new(
            id: "XMLESS003",
            title: "Complex types cannot be attributes",
            messageFormat: "Member '{0}.{1}' is mapped as an XML attribute",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS004: more than one simple-content member.
        /// </summary>
        internal static readonly DiagnosticDescriptor MultipleTextMembers = new(
            id: "XMLESS004",
            title: "Multiple text members",
            messageFormat: "The type '{0}' has more than one [XmlText] or [XmlCData]",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS005: more than one bindable constructor.
        /// </summary>
        internal static readonly DiagnosticDescriptor AmbiguousConstructors = new(
            id: "XMLESS005",
            title: "Ambiguous constructors",
            messageFormat: "The type '{0}' has more than one constructor that can bind its members",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS006: a mapped name is not a valid XML Name.
        /// </summary>
        internal static readonly DiagnosticDescriptor InvalidXmlName = new(
            id: "XMLESS006",
            title: "Invalid XML name",
            messageFormat: "The XML name '{0}' on '{1}' is not a valid XML Name",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS007: BCL XmlInclude or our XmlInclude on a type.
        /// </summary>
        internal static readonly DiagnosticDescriptor UseXmlDerivedType = new(
            id: "XMLESS007",
            title: "Use [XmlDerivedType]",
            messageFormat: "The type '{0}' uses XmlIncludeAttribute; use [XmlDerivedType] for known derived types",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS010: include/import is not an AdditionalFile, or the schema contains a DTD.
        /// </summary>
        internal static readonly DiagnosticDescriptor SchemaResolution = new(
            id: "XMLESS010",
            title: "Schema include or DTD is not allowed",
            messageFormat: "{0}",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS011: unsupported schema construct.
        /// </summary>
        internal static readonly DiagnosticDescriptor UnsupportedSchemaConstruct = new(
            id: "XMLESS011",
            title: "Unsupported schema construct",
            messageFormat: "{0}",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS012: generated C# name collision.
        /// </summary>
        internal static readonly DiagnosticDescriptor GeneratedNameCollision = new(
            id: "XMLESS012",
            title: "Generated name collision",
            messageFormat: "The generated C# name '{0}' is used more than once",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS013: generated member XML name is invalid.
        /// </summary>
        internal static readonly DiagnosticDescriptor GeneratedInvalidXmlName = new(
            id: "XMLESS013",
            title: "Invalid generated XML name",
            messageFormat: "The XML name '{0}' is not a valid XML Name",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS015: a complex type is mapped as the text of an element.
        /// </summary>
        internal static readonly DiagnosticDescriptor ComplexTypeAsText = new(
            id: "XMLESS015",
            title: "Complex types cannot be text",
            messageFormat: "Member '{0}.{1}' is mapped as XML text, and its type is not a single value",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS018: the schema allows something the generated types cannot read or write.
        /// </summary>
        internal static readonly DiagnosticDescriptor UnmappedSchemaConstruct = new(
            id: "XMLESS018",
            title: "Schema construct is not mapped",
            messageFormat: "{0}",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        /// <summary>
        /// XMLESS017: a required member is marked as never read.
        /// </summary>
        internal static readonly DiagnosticDescriptor RequiredMemberNeverRead = new(
            id: "XMLESS017",
            title: "Required member is never read",
            messageFormat: "'{0}' is required, and its ignore condition is XmlIgnoreCondition.WhenReading, so it would never be read",
            category: "XmlEssentials",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        #endregion

    }

}
