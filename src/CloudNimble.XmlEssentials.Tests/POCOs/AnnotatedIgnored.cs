using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose non-nullable members are left out when they are null or hold their default value.
    /// </summary>
    public sealed class AnnotatedIgnored
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value that is left out when it is the default.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingDefault)]
        public string Defaulted { get; set; } = "d";

        /// <summary>
        /// Gets or sets a value that is always written.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.Never)]
        public string Kept { get; set; } = "k";

        /// <summary>
        /// Gets or sets a value that is left out when it is null.
        /// </summary>
        [XmlIgnore(Condition = XmlIgnoreCondition.WhenWritingNull)]
        public string Skipped { get; set; } = "s";

        #endregion

    }

}
