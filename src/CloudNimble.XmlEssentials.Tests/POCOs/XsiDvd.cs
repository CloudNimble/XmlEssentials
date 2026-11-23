namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A disc whose <c>xsi:type</c> is <c>Dvd</c>.
    /// </summary>
    public sealed class XsiDvd : XsiProduct
    {

        #region Public Properties

        /// <summary>
        /// Gets the runtime in minutes.
        /// </summary>
        public int Runtime { get; set; }

        #endregion

    }

}
