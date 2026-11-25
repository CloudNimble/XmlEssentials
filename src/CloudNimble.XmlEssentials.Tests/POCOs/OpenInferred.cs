using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that asks for its derived types to be inferred and is not a closed type.
    /// </summary>
    [XmlPolymorphic(InferClosedTypePolymorphism = true)]
    public class OpenInferred
    {
    }

}
