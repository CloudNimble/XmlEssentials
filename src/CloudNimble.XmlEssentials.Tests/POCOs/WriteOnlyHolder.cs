using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a write-only property, which is not serialized even when included, and a name, which is.
    /// </summary>
    public sealed class WriteOnlyHolder
    {

        #region Public Properties

        /// <summary>
        /// Sets a value that is not serialized. <see cref="XmlIncludeAttribute"/> does not make a write-only member serializable.
        /// </summary>
        [XmlInclude]
        public string Hidden
        {
            set { }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

    }

}
