// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the tests.
#if NET11_0_OR_GREATER
namespace CloudNimble.XmlEssentials.Tests.POCOs.Closed
{

    /// <summary>
    /// The derived type of <see cref="ClosedKind"/>.
    /// </summary>
    public sealed class ClosedKindLeaf : ClosedKind
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value.
        /// </summary>
        public int Value { get; set; }

        #endregion

    }

}
#endif
