using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Resolves <see cref="XmlTypeInfo"/> for a CLR type.
    /// </summary>
    public interface IXmlTypeInfoResolver
    {

        /// <summary>
        /// Returns type info for <paramref name="type"/>, or <see langword="null"/> when this resolver does not handle it.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="options">The serializer options.</param>
        /// <returns>The type info, or <see langword="null"/>.</returns>
        XmlTypeInfo? GetTypeInfo(Type type, XmlSerializerOptions options);

    }

}
