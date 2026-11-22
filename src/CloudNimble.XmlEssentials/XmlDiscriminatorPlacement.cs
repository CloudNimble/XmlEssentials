namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Where a polymorphic type discriminator is written.
    /// </summary>
    public enum XmlDiscriminatorPlacement : byte
    {

        /// <summary>
        /// The derived type's element name is the discriminator.
        /// </summary>
        ElementName = 0,

        /// <summary>
        /// An attribute on the element.
        /// </summary>
        Attribute = 1,

        /// <summary>
        /// An <c>xsi:type</c> attribute.
        /// </summary>
        XsiType = 2,

    }

}
