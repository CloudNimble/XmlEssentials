using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A member whose XML name starts with white space, which is never part of a value.
    /// </summary>
    public enum PaddedName
    {

        /// <summary>
        /// The member.
        /// </summary>
        [XmlPropertyName(" x")]
        A,
    }

}
