namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies how a collection member without a mapping attribute is mapped: as repeated elements, or as one
    /// element that wraps an element for each item.
    /// </summary>
    public enum XmlCollectionMapping : byte
    {

        /// <summary>
        /// One element for each item, named for the member, with no element around them. This is the default.
        /// </summary>
        Unwrapped = 0,

        /// <summary>
        /// One element named for the member, which holds an element for each item, named for the item's type.
        /// This is the shape <c>System.Xml.Serialization.XmlSerializer</c> writes, and the one <c>[XmlArray]</c>
        /// gives a single member. A per-member <c>[XmlElement]</c> takes precedence and writes the items with
        /// no wrapper.
        /// </summary>
        Wrapped = 1,

    }

}
