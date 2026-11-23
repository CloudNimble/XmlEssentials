namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies where a polymorphic type discriminator is written.
    /// </summary>
    public enum XmlDiscriminatorPlacement : byte
    {

        /// <summary>
        /// The element name of the derived type, which serves as the discriminator.
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
