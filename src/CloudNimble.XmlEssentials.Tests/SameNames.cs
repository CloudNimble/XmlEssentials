using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Two members given the same XML name.
    /// </summary>
    public enum SameNames
    {

        /// <summary>
        /// The first member, which the shared name is read as.
        /// </summary>
        [XmlPropertyName("x")]
        A,

        /// <summary>
        /// The second member.
        /// </summary>
        [XmlPropertyName("x")]
        B,
    }

}
