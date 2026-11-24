// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the tests.
#if NET11_0_OR_GREATER
namespace CloudNimble.XmlEssentials.Tests.POCOs.Closed
{

    /// <summary>
    /// A derived type of <see cref="ClosedShape"/> that is closed itself, so it cannot be created.
    /// </summary>
    public closed class ClosedPolygon : ClosedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the number of sides.
        /// </summary>
        public int Sides { get; set; }

        #endregion

    }

}
#endif
