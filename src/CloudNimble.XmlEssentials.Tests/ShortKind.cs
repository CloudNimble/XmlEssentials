namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A <see cref="short"/> enum with members outside the range of one byte.
    /// </summary>
    public enum ShortKind : short
    {

        /// <summary>A negative value below <see cref="sbyte.MinValue"/>.</summary>
        Below = -300,

        /// <summary>A positive value above <see cref="byte.MaxValue"/>.</summary>
        Above = 300,

    }

}
