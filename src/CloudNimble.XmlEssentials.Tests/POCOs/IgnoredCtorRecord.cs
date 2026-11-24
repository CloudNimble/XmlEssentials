using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A positional record with constructor parameters whose properties are ignored.
    /// </summary>
    /// <param name="Name">The name, which is serialized.</param>
    /// <param name="Secret">A number that is never serialized.</param>
    /// <param name="Note">A string that is never serialized.</param>
    public sealed record IgnoredCtorRecord(string Name, [property: XmlIgnore] int Secret, [property: XmlIgnore] string? Note);

}
