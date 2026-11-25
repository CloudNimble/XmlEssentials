using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A struct with no setters, whose constructor is marked.
    /// </summary>
    public readonly struct MarkedCtorStruct
    {

        #region Public Properties

        /// <summary>
        /// Gets the X coordinate.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the Y coordinate.
        /// </summary>
        public int Y { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a value.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        [XmlConstructor]
        public MarkedCtorStruct(int x, int y)
        {
            X = x;
            Y = y;
        }

        #endregion

    }

}
