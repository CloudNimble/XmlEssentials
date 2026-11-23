using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Instructs the XmlEssentials source generator to generate source code for serializing and deserializing instances of
    /// the specified type and the types in its object graph. This class cannot be inherited.
    /// </summary>
    /// <example>
    /// <code>
    /// [XmlSerializable(typeof(Person))]
    /// internal partial class AppXmlContext : XmlSerializerContext
    /// {
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Place the attribute on a partial class that derives from <see cref="XmlSerializerContext"/>. This attribute is analogous
    /// to <c>JsonSerializableAttribute</c> in <c>System.Text.Json</c>.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class XmlSerializableAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the mode that indicates what the source generator generates for the type.
        /// </summary>
        /// <value>
        /// A bitwise combination of the enumeration values that specifies what is generated for the type. The default is
        /// <see cref="XmlSourceGenerationMode.Default"/>.
        /// </value>
        /// <remarks>
        /// The source generator does not use this property. The <see cref="XmlSourceGenerationOptionsAttribute.GenerationMode"/>
        /// of the context applies to every type.
        /// </remarks>
        public XmlSourceGenerationMode GenerationMode { get; set; }

        /// <summary>
        /// Gets the type to generate source code for.
        /// </summary>
        /// <value>
        /// The type that the context can serialize and deserialize.
        /// </value>
        public Type Type { get; }

        /// <summary>
        /// Gets or sets the name of the property for the generated <see cref="XmlTypeInfo{T}"/> for the type on the generated,
        /// derived <see cref="XmlSerializerContext"/> type.
        /// </summary>
        /// <value>
        /// The name of the generated property. If the value is <see langword="null"/> or empty, the name of the type is used.
        /// The default is <see langword="null"/>.
        /// </value>
        public string? TypeInfoPropertyName { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlSerializableAttribute"/> class with the specified type.
        /// </summary>
        /// <param name="type">The type to generate source code for.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="type"/> is <see langword="null"/>.
        /// </exception>
        public XmlSerializableAttribute(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            Type = type;
        }

        #endregion

    }

}
