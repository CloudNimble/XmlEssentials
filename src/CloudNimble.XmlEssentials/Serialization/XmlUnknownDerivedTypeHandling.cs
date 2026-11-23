namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// How an unknown derived type is handled when reading or writing a polymorphic type.
    /// </summary>
    public enum XmlUnknownDerivedTypeHandling
    {

        /// <summary>
        /// Throw when the type is not in the declared derived-type map.
        /// </summary>
        FailSerialization = 0,

        /// <summary>
        /// Read and write the value as the declared base type.
        /// </summary>
        FallBackToBaseType = 1,

        /// <summary>
        /// Read and write the value as the nearest declared ancestor.
        /// </summary>
        FallBackToNearestAncestor = 2,

    }

}
