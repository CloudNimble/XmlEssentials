namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies how a derived type that has not been explicitly declared for polymorphic serialization is handled when a
    /// polymorphic type is read or written.
    /// </summary>
    public enum XmlUnknownDerivedTypeHandling
    {

        /// <summary>
        /// Failure of the operation. An object of an undeclared runtime type, or an unknown type discriminator, causes an exception.
        /// </summary>
        FailSerialization = 0,

        /// <summary>
        /// Fallback to the serialization contract of the declared base type, on both reading and writing.
        /// </summary>
        FallBackToBaseType = 1,

        /// <summary>
        /// Fallback to the serialization contract of the nearest declared ancestor type on writing, or of the declared base type
        /// when no ancestor is declared. On reading, an unknown type discriminator falls back to the declared base type.
        /// </summary>
        FallBackToNearestAncestor = 2,

    }

}
