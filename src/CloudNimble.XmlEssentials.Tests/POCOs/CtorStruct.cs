using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A struct with a public constructor that takes parameters, which is not marked.
    /// </summary>
    public struct CtorStruct
    {

        #region Public Properties

        /// <summary>
        /// Gets whether the constructor with parameters created the value.
        /// </summary>
        [XmlIgnore]
        public bool ThroughConstructor { get; }

        /// <summary>
        /// Gets or sets the X coordinate.
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// Gets or sets the Y coordinate.
        /// </summary>
        public int Y { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a value.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="y">The Y coordinate.</param>
        public CtorStruct(int x, int y)
        {
            X = x;
            Y = y;
            ThroughConstructor = true;
        }

        #endregion

    }

}
