using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// How floating-point named literals are treated.
    /// </summary>
    [Flags]
    public enum XmlNumberHandling
    {

        /// <summary>
        /// Reject <c>INF</c>, <c>-INF</c>, and <c>NaN</c>.
        /// </summary>
        Strict = 0,

        /// <summary>
        /// Accept and write the XSD named literals (any letter case on read).
        /// </summary>
        AllowNamedFloatingPointLiterals = 1,

    }

}
