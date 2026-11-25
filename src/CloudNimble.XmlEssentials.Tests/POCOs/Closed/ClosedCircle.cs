// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the tests.
#if NET11_0_OR_GREATER
namespace CloudNimble.XmlEssentials.Tests.POCOs.Closed
{

    /// <summary>
    /// A derived type of <see cref="ClosedShape"/>.
    /// </summary>
    public sealed class ClosedCircle : ClosedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        public int Radius { get; set; }

        #endregion

    }

}
#endif
