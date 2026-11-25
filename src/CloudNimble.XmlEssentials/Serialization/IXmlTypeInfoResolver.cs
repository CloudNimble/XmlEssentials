using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Defines a resolver of the XML serialization contract for requested types.
    /// </summary>
    public interface IXmlTypeInfoResolver
    {

        /// <summary>
        /// Resolves an <see cref="XmlTypeInfo"/> contract for the requested type and options.
        /// </summary>
        /// <param name="type">The type to resolve a contract for.</param>
        /// <param name="options">The options that the contract is resolved for.</param>
        /// <returns>
        /// The contract that matches <paramref name="type"/>, or <see langword="null"/> if this resolver does not handle the type.
        /// </returns>
        XmlTypeInfo? GetTypeInfo(Type type, XmlSerializerOptions options);

    }

}
