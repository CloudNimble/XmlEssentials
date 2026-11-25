using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that keeps its unmapped members in a field.
    /// </summary>
    public sealed class FieldExtra
    {

        #region Fields

        /// <summary>
        /// The unmapped members.
        /// </summary>
        [XmlExtensionData]
        public XmlExtensionData Extra = new();

        /// <summary>
        /// The name.
        /// </summary>
        public string? Name;

        #endregion

    }

}
