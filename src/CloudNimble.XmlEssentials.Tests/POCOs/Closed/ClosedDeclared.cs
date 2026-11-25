// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the tests.
#if NET11_0_OR_GREATER
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Closed
{

    /// <summary>
    /// A closed type that declares one of its two derived types, so nothing is inferred for it.
    /// </summary>
    [XmlDerivedType<ClosedDeclaredFirst>("first")]
    public closed class ClosedDeclared
    {
    }

}
#endif
