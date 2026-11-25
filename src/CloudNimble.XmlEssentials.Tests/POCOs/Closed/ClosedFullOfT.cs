// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the tests.
#if NET11_0_OR_GREATER
namespace CloudNimble.XmlEssentials.Tests.POCOs.Closed
{

    /// <summary>
    /// A derived type that takes the type argument of <see cref="ClosedBox{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of what the box holds.</typeparam>
    public sealed class ClosedFull<T> : ClosedBox<T>
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets what the box holds.
        /// </summary>
        public T? Value { get; set; }

        #endregion

    }

}
#endif
