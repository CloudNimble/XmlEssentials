using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A flags member whose XML name contains the separator of flags names.
    /// </summary>
    [Flags]
    public enum SpacedFlag
    {

        /// <summary>
        /// The member.
        /// </summary>
        [XmlPropertyName("a b")]
        A = 1,
    }

}
