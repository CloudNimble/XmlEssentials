using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Two members whose names differ only in case.
    /// </summary>
    public enum CaseTwins
    {

        /// <summary>
        /// The first member, which a name in any other case is read as.
        /// </summary>
        Ab,

        /// <summary>
        /// The second member, read only from its exact name.
        /// </summary>
#pragma warning disable CA1708, IDE1006
        aB,
#pragma warning restore CA1708, IDE1006
    }

}
