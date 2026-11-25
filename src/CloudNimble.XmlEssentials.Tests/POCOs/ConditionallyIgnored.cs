using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose members are omitted when they are null or default, via <see cref="XmlIgnoreAttribute"/>.
    /// </summary>
    public sealed class ConditionallyIgnored
    {

        #region Public Properties

        /// <summary>
        /// Gets the given name. Omitted when null.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNull)]
        public string? Name { get; set; }

        /// <summary>
        /// Gets the age. Omitted when zero.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingDefault)]
        public int Age { get; set; }

        #endregion

    }

}
