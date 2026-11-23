namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Keeps a value-type member in a box that is written again for the next object, while a type created
    /// through a constructor with parameters is being read.
    /// </summary>
    /// <typeparam name="TValue">The member type.</typeparam>
    /// <remarks>
    /// The constructor is called through reflection, which takes each argument as an <see cref="object"/>.
    /// Boxing every value as it is read would allocate one object per value-type member per instance. The
    /// constructor copies the value out of the box, so the box can hold the next instance's value.
    /// </remarks>
    internal abstract class XmlHeldBox<TValue>
    {

        #region Private Methods

        /// <summary>
        /// Creates a box holding the default value.
        /// </summary>
        /// <returns>A new box that nothing else refers to.</returns>
        internal abstract object Create();

        /// <summary>
        /// Writes <paramref name="value"/> into <paramref name="box"/>.
        /// </summary>
        /// <param name="box">A box made by <see cref="Create"/>.</param>
        /// <param name="value">The value.</param>
        internal abstract void Set(object box, TValue value);

        #endregion

    }

}
