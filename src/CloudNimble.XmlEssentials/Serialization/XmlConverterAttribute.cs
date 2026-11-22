using System;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies a converter for a type or member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlConverterAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets the converter type.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public Type ConverterType { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="converterType">A type that derives from <see cref="XmlConverter"/>.</param>
        public XmlConverterAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type converterType)
        {
            ArgumentNullException.ThrowIfNull(converterType);
            ConverterType = converterType;
        }

        #endregion

    }

}
