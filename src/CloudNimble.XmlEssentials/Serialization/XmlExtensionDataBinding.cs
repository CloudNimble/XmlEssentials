namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Accessors for a type's <see cref="XmlExtensionData"/> member.
    /// </summary>
    /// <remarks>
    /// The serializer holds the object it reads or writes as an <see cref="object"/>. The accessors are typed by
    /// the type that declares the member, so the class that holds them is generic over it and this is the base
    /// the engine calls.
    /// </remarks>
    internal abstract class XmlExtensionDataBinding
    {

        #region Public Methods

        /// <summary>
        /// Reads the bag from <paramref name="obj"/>.
        /// </summary>
        /// <param name="obj">The instance.</param>
        /// <returns>The bag, or null.</returns>
        internal abstract XmlExtensionData? GetValue(object obj);

        /// <summary>
        /// Assigns <paramref name="value"/> on <paramref name="obj"/> when the member can be set.
        /// </summary>
        /// <param name="obj">The instance.</param>
        /// <param name="value">The bag.</param>
        internal abstract void SetValue(object obj, XmlExtensionData value);

        #endregion

    }

}
