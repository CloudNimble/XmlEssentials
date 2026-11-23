using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies how <see cref="XmlSerializer"/> handles the named floating-point literals <c>INF</c>, <c>-INF</c>, and <c>NaN</c>
    /// when serializing and deserializing.
    /// </summary>
    [Flags]
    public enum XmlNumberHandling
    {

        /// <summary>
        /// Strict handling, in which <c>INF</c>, <c>-INF</c>, and <c>NaN</c> are rejected on both reading and writing.
        /// </summary>
        Strict = 0,

        /// <summary>
        /// Lenient handling, in which the XML Schema named literals <c>INF</c>, <c>-INF</c>, and <c>NaN</c> are read, in any letter case, and written.
        /// </summary>
        AllowNamedFloatingPointLiterals = 1,

    }

}
