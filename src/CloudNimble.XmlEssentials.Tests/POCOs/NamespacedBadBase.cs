using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A base type that gives a derived type a namespace when its discriminator is not an element name.
    /// </summary>
    [XmlPolymorphic(DiscriminatorPlacement = XmlDiscriminatorPlacement.XsiType)]
    [XmlDerivedType<NamespacedBadLeaf>("leaf", Namespace = "urn:leaf")]
    public class NamespacedBadBase
    {
    }

}
