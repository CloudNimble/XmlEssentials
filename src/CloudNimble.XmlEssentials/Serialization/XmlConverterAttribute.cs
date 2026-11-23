using System;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the converter type to use when placed on a property, field, or type. This class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlConverterAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets the type of the converter to use.
        /// </summary>
        /// <value>
        /// The type of the converter, which derives from <see cref="XmlConverter"/>.
        /// </value>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public Type ConverterType { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlConverterAttribute"/> class with the specified converter type.
        /// </summary>
        /// <param name="converterType">The type of the converter. It must derive from <see cref="XmlConverter"/> and have a public parameterless constructor.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="converterType"/> is <see langword="null"/>.
        /// </exception>
        public XmlConverterAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type converterType)
        {
            ArgumentNullException.ThrowIfNull(converterType);
            ConverterType = converterType;
        }

        #endregion

    }

}
