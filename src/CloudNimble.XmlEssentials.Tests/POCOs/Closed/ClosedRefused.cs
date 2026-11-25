// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the tests.
#if NET11_0_OR_GREATER
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Closed
{

    /// <summary>
    /// A closed type that refuses to have its derived types inferred, whatever the options say.
    /// </summary>
    [XmlPolymorphic(InferClosedTypePolymorphism = false)]
    public closed class ClosedRefused
    {
    }

}
#endif
