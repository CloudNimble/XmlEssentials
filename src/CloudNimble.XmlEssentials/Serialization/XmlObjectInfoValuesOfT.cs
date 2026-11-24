using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Provides serialization metadata about an object type with constructors, properties, and fields. This class
    /// cannot be inherited.
    /// </summary>
    /// <typeparam name="T">
    /// The object type to serialize or deserialize.
    /// </typeparam>
    /// <remarks>
    /// Source-generated contexts create instances of this class and pass them to
    /// <see cref="XmlMetadataServices.CreateObjectInfo{T}"/>. It is analogous to <c>JsonObjectInfoValues&lt;T&gt;</c> in
    /// <c>System.Text.Json</c>. The serializer reads and writes the type from this metadata with the rules it applies
    /// to a type it maps through reflection, and with no reflection over the members of the type.
    /// </remarks>
    public sealed class XmlObjectInfoValues<T>
    {

        #region Public Properties

        /// <summary>
        /// Gets or initializes a method that returns the metadata of the parameters of the constructor that
        /// <see cref="ObjectWithParameterizedConstructorCreator"/> calls.
        /// </summary>
        /// <value>
        /// A method that returns one <see cref="XmlParameterInfoValues"/> for each parameter, or
        /// <see langword="null"/> when the type is not created through a constructor with parameters.
        /// </value>
        public Func<XmlParameterInfoValues[]>? ConstructorParameterMetadataInitializer { get; init; }

        /// <summary>
        /// Gets or initializes a mechanism to create an instance of the type with no arguments.
        /// </summary>
        /// <value>
        /// A method that creates the object, or <see langword="null"/> when the type is created through
        /// <see cref="ObjectWithParameterizedConstructorCreator"/> or is never created, as an abstract class is not.
        /// </value>
        public Func<T>? ObjectCreator { get; init; }

        /// <summary>
        /// Gets or initializes a mechanism to create an instance of the type through a constructor with parameters.
        /// </summary>
        /// <value>
        /// A method that creates the object from an array that holds one argument for each parameter, in the order
        /// of <see cref="XmlParameterInfoValues.Position"/>; or <see langword="null"/>. The array may be longer than
        /// the parameter list.
        /// </value>
        public Func<object?[], T>? ObjectWithParameterizedConstructorCreator { get; init; }

        /// <summary>
        /// Gets or initializes a method that returns the metadata of the properties and fields of the type.
        /// </summary>
        /// <value>
        /// A method that returns one <see cref="XmlPropertyInfoValues"/> for each property and field of the type and
        /// its base types, in the order they are declared, base types first. The serializer leaves out the ones the
        /// options and the attributes exclude; or <see langword="null"/> when the type has no metadata, as in
        /// <see cref="XmlSourceGenerationMode.Serialization"/>.
        /// </value>
        public Func<XmlSerializerOptions, XmlPropertyInfoValues[]>? PropertyMetadataInitializer { get; init; }

        /// <summary>
        /// Gets or initializes a method that writes the members of an object directly, without the metadata.
        /// </summary>
        /// <value>
        /// A method that writes the attributes and the content of the element for a value, or <see langword="null"/>.
        /// The start and end tags are written by the serializer.
        /// </value>
        /// <remarks>
        /// A source-generated context sets this only when <see cref="XmlMetadataServices.CanUseSerializeHandler"/>
        /// says the options allow it. When it is set, the serializer writes through it. When
        /// <see cref="PropertyMetadataInitializer"/> is <see langword="null"/> as well, the type has no metadata: it can
        /// be written through this method and cannot be read.
        /// </remarks>
        public Action<Utf8XmlWriter, T, XmlSerializerOptions>? SerializeHandler { get; init; }

        #endregion

    }

}
