using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Simple content whose value is a nullable scalar.
    /// </summary>
    public sealed class NullableText
    {

        #region Public Properties

        /// <summary>
        /// Gets the value.
        /// </summary>
        [XmlText]
        public int? Value { get; set; }

        #endregion

    }

}
