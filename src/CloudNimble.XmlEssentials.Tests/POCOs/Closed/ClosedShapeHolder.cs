// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the tests.
#if NET11_0_OR_GREATER
namespace CloudNimble.XmlEssentials.Tests.POCOs.Closed
{

    /// <summary>
    /// Holds closed shapes as a member and as the items of a collection in a wrapper.
    /// </summary>
    public sealed class ClosedShapeHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets one shape.
        /// </summary>
        public ClosedShape? Shape { get; set; }

        #endregion

    }

}
#endif
