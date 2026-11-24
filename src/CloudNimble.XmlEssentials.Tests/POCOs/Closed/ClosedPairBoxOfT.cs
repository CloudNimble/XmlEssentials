// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the tests.
#if NET11_0_OR_GREATER
using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Closed
{

    /// <summary>
    /// A derived type that takes two arguments, where <see cref="ClosedBox{T}"/> takes one.
    /// </summary>
    /// <typeparam name="TKey">The type of a key.</typeparam>
    /// <typeparam name="TValue">The type of a value.</typeparam>
    public sealed class ClosedPairBox<TKey, TValue> : ClosedBox<KeyValuePair<TKey, TValue>>
    {
    }

}
#endif
