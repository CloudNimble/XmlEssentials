namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A <see cref="long"/> enum with members outside the range of <see cref="int"/>.
    /// </summary>
    public enum LongCode : long
    {

        /// <summary>The smallest value of the underlying type.</summary>
        Smallest = long.MinValue,

        /// <summary>A value above <see cref="uint.MaxValue"/>.</summary>
        Big = 5_000_000_000,

    }

}
