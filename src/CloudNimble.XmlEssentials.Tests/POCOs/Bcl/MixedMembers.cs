using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Bcl
{

    /// <summary>
    /// A class with fields and properties, derived from one that has both, and one member with an explicit order.
    /// </summary>
    public class MixedMembers : MixedBase
    {

        #region Fields

        /// <summary>
        /// The first field.
        /// </summary>
        public int F1 = 4;

        /// <summary>
        /// The second field.
        /// </summary>
        public int F2 = 6;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a property that is written first, whatever the order of the others.
        /// </summary>
        [XmlPropertyOrder(-1)]
        public int Lead { get; set; } = 7;

        /// <summary>
        /// Gets or sets the first property.
        /// </summary>
        public int P1 { get; set; } = 3;

        /// <summary>
        /// Gets or sets the second property.
        /// </summary>
        public int P2 { get; set; } = 5;

        #endregion

    }

}
