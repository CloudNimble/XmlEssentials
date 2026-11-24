namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A weight in grams. It has no converter of its own: <see cref="WeightConverter"/> is added to the options.
    /// </summary>
    /// <param name="Grams">The weight in grams.</param>
    public readonly record struct Weight(int Grams);

}
