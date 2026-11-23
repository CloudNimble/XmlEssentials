using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that serializes a non-public property through <see cref="XmlIncludeAttribute"/>.
    /// </summary>
    public sealed class PrivateHolder
    {

        #region Public Properties

        /// <summary>
        /// Gets the public name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets a non-public value that is included in the document.
        /// </summary>
        [XmlInclude]
        internal string? Hidden { get; set; }

        #endregion

    }

}
