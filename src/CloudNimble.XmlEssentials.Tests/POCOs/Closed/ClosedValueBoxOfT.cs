// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the tests.
#if NET11_0_OR_GREATER
namespace CloudNimble.XmlEssentials.Tests.POCOs.Closed
{

    /// <summary>
    /// A derived type that can take only a value type as its argument.
    /// </summary>
    /// <typeparam name="T">The type of what the box holds.</typeparam>
    public sealed class ClosedValueBox<T> : ClosedBox<T>
        where T : struct
    {
    }

}
#endif
