using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A base type whose derived types are named by elements in namespaces of their own.
    /// </summary>
    [XmlDerivedType<NamespacedCircle>("shape", Namespace = "urn:round")]
    [XmlDerivedType<NamespacedSquare>("shape", Namespace = "urn:square")]
    [XmlDerivedType<NamespacedBlob>("blob")]
    [XmlDerivedType<NamespacedDot>("dot", Namespace = "")]
    public abstract class NamespacedShape
    {
    }

}
