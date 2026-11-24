using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Paths
{

    /// <summary>
    /// A base type whose derived type is written under an element name of its own.
    /// </summary>
    [XmlDerivedType<PathCircle>("circle")]
    public abstract class PathShape
    {
    }

}
