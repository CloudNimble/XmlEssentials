using System;
using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Defines the default, reflection-based XML contract resolver used by <see cref="XmlSerializer"/>. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// The resolver builds type info from the converters of an <see cref="XmlSerializerOptions"/> instance: its
    /// <see cref="XmlSerializerOptions.Converters"/>, the <see cref="XmlConverterAttribute"/> on the type, and the built-in converters.
    /// </remarks>
    [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
    public sealed class DefaultXmlTypeInfoResolver : IXmlTypeInfoResolver
    {

        #region Public Properties

        /// <summary>
        /// Gets the shared instance of the <see cref="DefaultXmlTypeInfoResolver"/> class.
        /// </summary>
        /// <value>
        /// The shared resolver.
        /// </value>
        public static DefaultXmlTypeInfoResolver Instance { get; } = new();

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the resolver.
        /// </summary>
        /// <remarks>
        /// Creating the resolver is what lets the serializer look at the interfaces of a type and close a generic
        /// type at run time. A program that never creates it has none of that code, which is what the trimmer
        /// and the Native AOT compiler need.
        /// </remarks>
        internal DefaultXmlTypeInfoResolver()
        {
            XmlCollectionShape.UseReflection();
#if NET11_0_OR_GREATER
            XmlObjectBinder.UseReflection();
#endif
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException">
        /// <paramref name="type"/> or <paramref name="options"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The converter found for <paramref name="type"/> does not convert exactly that type.
        /// </exception>
        public XmlTypeInfo? GetTypeInfo(Type type, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(options);
            if (options.GetConverterOrNull(type) is not { } converter)
            {
                return null;
            }

            return converter.CreateTypeInfo(type, options)
                ?? throw new InvalidOperationException($"The converter '{converter.GetType()}' is not compatible with the type '{type}'.");
        }

        #endregion

    }

}
