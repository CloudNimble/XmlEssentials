using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Declares a type that a source-generated <see cref="XmlSerializerContext"/> can serialize.
    /// Analogous to <c>JsonSerializableAttribute</c>.
    /// </summary>
    /// <example>
    /// <code>
    /// [XmlSerializable(typeof(Person))]
    /// internal partial class AppXmlContext : XmlSerializerContext
    /// {
    /// }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class XmlSerializableAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the generation mode for this type. When <see cref="XmlSourceGenerationMode.Default"/>,
        /// the context's <see cref="XmlSourceGenerationOptionsAttribute.GenerationMode"/> is used.
        /// </summary>
        public XmlSourceGenerationMode GenerationMode { get; set; }

        /// <summary>
        /// Gets the CLR type to serialize.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets or sets the generated <c>XmlTypeInfo&lt;T&gt;</c> property name. When null, the type name is used.
        /// </summary>
        public string? TypeInfoPropertyName { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the attribute.
        /// </summary>
        /// <param name="type">The CLR type to serialize.</param>
        public XmlSerializableAttribute(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);
            Type = type;
        }

        #endregion

    }

}
