using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Instructs the XmlEssentials source generator to generate source code for serializing and deserializing instances of
    /// the specified type and the types in its object graph. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="T">
    /// The type to generate source code for.
    /// </typeparam>
    /// <example>
    /// <code>
    /// [XmlSerializable&lt;Person&gt;]
    /// [XmlSerializable&lt;Person[]&gt;]
    /// [XmlSerializable&lt;Dictionary&lt;string, Person&gt;&gt;(TypeInfoPropertyName = "PeopleByName")]
    /// internal partial class AppXmlContext : XmlSerializerContext
    /// {
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// Place the attribute on a partial class that derives from <see cref="XmlSerializerContext"/>. The type is a type
    /// argument, so the compiler checks it and it may be any closed type, an array or a collection included. This attribute
    /// is analogous to <c>JsonSerializableAttribute</c> in <c>System.Text.Json</c>, which takes the type as a constructor
    /// argument.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class XmlSerializableAttribute<T> : Attribute
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
        /// A value other than <see cref="XmlSourceGenerationMode.Default"/> overrides the
        /// <see cref="XmlSourceGenerationOptionsAttribute.GenerationMode"/> of the context for this type. The types reached
        /// from it take the mode of the context.
        /// </remarks>
        public XmlSourceGenerationMode GenerationMode { get; set; }

        /// <summary>
        /// Gets the type to generate source code for.
        /// </summary>
        /// <value>
        /// The type that the context can serialize and deserialize, which is <typeparamref name="T"/>.
        /// </value>
        public Type Type => typeof(T);

        /// <summary>
        /// Gets or sets the name of the property for the generated <see cref="XmlTypeInfo{T}"/> for the type on the generated,
        /// derived <see cref="XmlSerializerContext"/> type.
        /// </summary>
        /// <value>
        /// The name of the generated property. If the value is <see langword="null"/> or empty, the name is made from the
        /// name of the type and the names of its type arguments. The default is <see langword="null"/>.
        /// </value>
        public string? TypeInfoPropertyName { get; set; }

        #endregion

    }

}
