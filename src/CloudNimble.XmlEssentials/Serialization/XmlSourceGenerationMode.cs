using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the generation mode for the XmlEssentials source generator.
    /// </summary>
    [Flags]
    public enum XmlSourceGenerationMode
    {

        /// <summary>
        /// The default mode, in which both type metadata and serialization logic are generated. The result is the same as
        /// combining <see cref="XmlSourceGenerationMode.Metadata"/> and <see cref="XmlSourceGenerationMode.Serialization"/>.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Type-metadata initialization logic only. No converters are generated.
        /// </summary>
        Metadata = 1,

        /// <summary>
        /// Generated converters that serialize and deserialize the types of the context.
        /// </summary>
        Serialization = 2,

    }

}
