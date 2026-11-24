// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the tests.
#if NET11_0_OR_GREATER
namespace CloudNimble.XmlEssentials.Tests.POCOs.Closed
{

    /// <summary>
    /// A closed generic type.
    /// </summary>
    /// <typeparam name="T">The type of what a box holds.</typeparam>
    public closed class ClosedBox<T>
    {
    }

}
#endif
