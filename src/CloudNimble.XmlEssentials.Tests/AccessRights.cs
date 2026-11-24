using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Flags of which one has an XML name of its own.
    /// </summary>
    [Flags]
    public enum AccessRights
    {

        /// <summary>
        /// No access.
        /// </summary>
        None = 0,

        /// <summary>
        /// May read. Written as <c>rd</c>.
        /// </summary>
        [XmlPropertyName("rd")]
        Read = 1,

        /// <summary>
        /// May write.
        /// </summary>
        Write = 2,
    }

}
