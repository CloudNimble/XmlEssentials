using System;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A <see cref="ulong"/> flags enum with no zero member, a composite member, and a member in the top bit.
    /// </summary>
    [Flags]
    public enum WideFlags : ulong
    {

        /// <summary>Bit 0.</summary>
        Low = 1,

        /// <summary>Bit 1.</summary>
        Mid = 2,

        /// <summary>Bits 0 and 1.</summary>
        LowMid = 3,

        /// <summary>Bit 63, which does not fit in <see cref="long"/>.</summary>
        High = 1UL << 63,

    }

}
