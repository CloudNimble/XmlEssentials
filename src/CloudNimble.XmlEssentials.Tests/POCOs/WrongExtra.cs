using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose extension-data member is the wrong CLR type.
    /// </summary>
    public sealed class WrongExtra
    {

        #region Public Properties

        /// <summary>
        /// Gets a member incorrectly marked as extension data.
        /// </summary>
        [XmlExtensionData]
        public string? Extra { get; set; }

        #endregion

    }

}
