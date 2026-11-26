namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// A point with a label: a value type with properties and a field, read and written as an object.
    /// </summary>
    public struct Vertex
    {

        #region Fields

        /// <summary>
        /// The flags of the point. A field, which is read and written when fields are included.
        /// </summary>
        public int Flags;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string? Label { get; set; }

        /// <summary>
        /// Gets or sets the horizontal position.
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Gets or sets the vertical position.
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        public double Z { get; set; }

        #endregion

    }

}
