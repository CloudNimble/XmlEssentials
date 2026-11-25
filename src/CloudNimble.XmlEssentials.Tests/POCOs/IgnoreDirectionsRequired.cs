using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a required member that is marked as never read, which cannot be satisfied.
    /// </summary>
    public sealed class IgnoreDirectionsRequired
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a required value that is never read.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenReading)]
        public required string Name { get; set; }

        #endregion

    }

}
