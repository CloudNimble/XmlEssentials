namespace CloudNimble.XmlEssentials.Tests.POCOs.Paths
{

    /// <summary>
    /// The derived type of <see cref="PathShape"/>.
    /// </summary>
    public sealed class PathCircle : PathShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value that a converter reads and writes.
        /// </summary>
        public PathOdd? Odd { get; set; }

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        public int Radius { get; set; }

        #endregion

    }

}
