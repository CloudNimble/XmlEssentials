namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type created through a constructor with one non-nullable and one nullable parameter.
    /// </summary>
    /// <param name="Name">A value annotated as never null.</param>
    /// <param name="Maybe">A value that may be null.</param>
    public sealed record AnnotatedRecord(string Name, string? Maybe);

}
