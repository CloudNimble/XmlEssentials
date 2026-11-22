using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// What the source generator emits for a context.
    /// </summary>
    [Flags]
    public enum XmlSourceGenerationMode
    {

        /// <summary>
        /// Metadata and serialization (<c>Metadata | Serialization</c>).
        /// </summary>
        Default = 0,

        /// <summary>
        /// Type info only.
        /// </summary>
        Metadata = 1,

        /// <summary>
        /// Serialize and deserialize methods.
        /// </summary>
        Serialization = 2,

    }

}
