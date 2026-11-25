using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the generation mode for the XmlEssentials source generator.
    /// </summary>
    /// <remarks>
    /// Type metadata describes the constructor and the members of a type. The serializer reads and writes the type from
    /// it with the rules it applies to a type it maps through reflection, and with no reflection over the members.
    /// Serialization logic is a write fast path: a method that writes the type directly, which is used when the options
    /// agree with the <see cref="XmlSourceGenerationOptionsAttribute"/> of the context. Every mode is safe for trimming
    /// and Native AOT. This enumeration is analogous to <c>JsonSourceGenerationMode</c> in <c>System.Text.Json</c>.
    /// </remarks>
    [Flags]
    public enum XmlSourceGenerationMode
    {

        /// <summary>
        /// The default mode, in which both type metadata and serialization logic are generated. The result is the same as
        /// combining <see cref="XmlSourceGenerationMode.Metadata"/> and <see cref="XmlSourceGenerationMode.Serialization"/>.
        /// The write fast path writes when the options allow it, and the serializer does everything else from the metadata.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Type-metadata initialization logic only. The serializer reads and writes the types of the context from the metadata.
        /// </summary>
        Metadata = 1,

        /// <summary>
        /// Serialization logic only: a write fast path and no type metadata. A type can be written with the options the
        /// context was compiled for. Every read, and a write with other options, throws <see cref="InvalidOperationException"/>,
        /// and <see cref="XmlTypeInfo.Properties"/> is empty.
        /// </summary>
        Serialization = 2,

    }

}
