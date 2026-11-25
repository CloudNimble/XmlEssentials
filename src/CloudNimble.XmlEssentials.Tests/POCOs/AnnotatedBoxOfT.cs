namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A generic type whose member has the type of the type parameter, which says nothing about null.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public sealed class AnnotatedBox<T>
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public T Value { get; set; } = default!;

        #endregion

    }

}
