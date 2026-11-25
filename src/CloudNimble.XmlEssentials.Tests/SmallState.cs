namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A <see cref="byte"/> enum whose largest member uses every bit.
    /// </summary>
    public enum SmallState : byte
    {

        /// <summary>The zero member.</summary>
        Off = 0,

        /// <summary>One.</summary>
        On = 1,

        /// <summary>The largest value of the underlying type.</summary>
        Max = 255,

    }

}
