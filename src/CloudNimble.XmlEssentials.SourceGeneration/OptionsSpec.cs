using System;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// Seed options from <c>[XmlSourceGenerationOptions]</c>.
    /// </summary>
    internal readonly struct OptionsSpec : IEquatable<OptionsSpec>
    {

        #region Public Properties

        /// <summary>
        /// Gets the default namespace URI.
        /// </summary>
        internal string DefaultNamespace { get; }

        /// <summary>
        /// Gets the default ignore condition.
        /// </summary>
        internal int DefaultIgnoreCondition { get; }

        /// <summary>
        /// Gets the default property mapping.
        /// </summary>
        internal int DefaultPropertyMapping { get; }

        /// <summary>
        /// Gets the element naming policy.
        /// </summary>
        internal int ElementNamingPolicy { get; }

        /// <summary>
        /// Gets the attribute naming policy.
        /// </summary>
        internal int AttributeNamingPolicy { get; }

        /// <summary>
        /// Gets the property naming policy.
        /// </summary>
        internal int PropertyNamingPolicy { get; }

        /// <summary>
        /// Gets whether public fields are included.
        /// </summary>
        internal bool IncludeFields { get; }

        /// <summary>
        /// Gets whether a null for a member annotated non-nullable is refused.
        /// </summary>
        internal bool RespectNullableAnnotations { get; }

        /// <summary>
        /// Gets the object creation handling value.
        /// </summary>
        internal int PreferredObjectCreationHandling { get; }

        /// <summary>
        /// Gets the unknown type handling value.
        /// </summary>
        internal int UnknownTypeHandling { get; }

        /// <summary>
        /// Gets whether the XML declaration is omitted.
        /// </summary>
        internal bool OmitXmlDeclaration { get; }

        /// <summary>
        /// Gets whether output is indented.
        /// </summary>
        internal bool WriteIndented { get; }

        /// <summary>
        /// Gets whether a seed was specified.
        /// </summary>
        internal bool HasSeed { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the options spec.
        /// </summary>
        /// <param name="hasSeed">Whether an attribute was present.</param>
        /// <param name="writeIndented">Whether output is indented.</param>
        /// <param name="omitXmlDeclaration">Whether the declaration is omitted.</param>
        /// <param name="defaultNamespace">The default namespace.</param>
        /// <param name="propertyNamingPolicy">The property naming policy.</param>
        /// <param name="elementNamingPolicy">The element naming policy.</param>
        /// <param name="attributeNamingPolicy">The attribute naming policy.</param>
        /// <param name="defaultIgnoreCondition">The ignore condition.</param>
        /// <param name="defaultPropertyMapping">The property mapping.</param>
        /// <param name="includeFields">Whether fields are included.</param>
        /// <param name="respectNullableAnnotations">Whether a null for a member annotated non-nullable is refused.</param>
        /// <param name="preferredObjectCreationHandling">The object creation handling.</param>
        /// <param name="unknownTypeHandling">The unknown type handling.</param>
        internal OptionsSpec(
            bool hasSeed,
            bool writeIndented,
            bool omitXmlDeclaration,
            string defaultNamespace,
            int propertyNamingPolicy,
            int elementNamingPolicy,
            int attributeNamingPolicy,
            int defaultIgnoreCondition,
            int defaultPropertyMapping,
            bool includeFields,
            bool respectNullableAnnotations,
            int preferredObjectCreationHandling,
            int unknownTypeHandling)
        {
            HasSeed = hasSeed;
            WriteIndented = writeIndented;
            OmitXmlDeclaration = omitXmlDeclaration;
            DefaultNamespace = defaultNamespace;
            PropertyNamingPolicy = propertyNamingPolicy;
            ElementNamingPolicy = elementNamingPolicy;
            AttributeNamingPolicy = attributeNamingPolicy;
            DefaultIgnoreCondition = defaultIgnoreCondition;
            DefaultPropertyMapping = defaultPropertyMapping;
            IncludeFields = includeFields;
            RespectNullableAnnotations = respectNullableAnnotations;
            PreferredObjectCreationHandling = preferredObjectCreationHandling;
            UnknownTypeHandling = unknownTypeHandling;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Equals(OptionsSpec other)
        {
            return HasSeed == other.HasSeed
                && WriteIndented == other.WriteIndented
                && OmitXmlDeclaration == other.OmitXmlDeclaration
                && DefaultNamespace == other.DefaultNamespace
                && PropertyNamingPolicy == other.PropertyNamingPolicy
                && ElementNamingPolicy == other.ElementNamingPolicy
                && AttributeNamingPolicy == other.AttributeNamingPolicy
                && DefaultIgnoreCondition == other.DefaultIgnoreCondition
                && DefaultPropertyMapping == other.DefaultPropertyMapping
                && IncludeFields == other.IncludeFields
                && RespectNullableAnnotations == other.RespectNullableAnnotations
                && PreferredObjectCreationHandling == other.PreferredObjectCreationHandling
                && UnknownTypeHandling == other.UnknownTypeHandling;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is OptionsSpec other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return DefaultNamespace.GetHashCode() ^ PropertyNamingPolicy;
        }

        #endregion

    }

}
