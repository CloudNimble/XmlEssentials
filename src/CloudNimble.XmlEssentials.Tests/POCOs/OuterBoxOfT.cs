namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A generic type with a nested type, which is generic through its parent and has no arity in its own name.
    /// </summary>
    /// <typeparam name="T">The type of the value a part holds.</typeparam>
    public sealed class OuterBox<T>
    {

        /// <summary>
        /// A part of the box.
        /// </summary>
        public sealed class Part
        {

            #region Public Properties

            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            public T? Value { get; set; }

            #endregion

        }

    }

}
