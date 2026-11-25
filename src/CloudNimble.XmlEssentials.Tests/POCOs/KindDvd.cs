namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A disc whose kind discriminator is <c>dvd</c>.
    /// </summary>
    public sealed class KindDvd : KindProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets the runtime in minutes.
        /// </summary>
        public int Runtime { get; set; }

        #endregion

    }

}
