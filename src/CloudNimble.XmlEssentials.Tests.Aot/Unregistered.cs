namespace CloudNimble.XmlEssentials.Tests.Aot
{

    /// <summary>
    /// A type that is not in any <c>XmlSerializerContext</c>, so reflection-off serialize must fail.
    /// </summary>
    public sealed class Unregistered
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value.
        /// </summary>
        public int N { get; set; }

        #endregion

    }

}
