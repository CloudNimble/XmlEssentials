namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A DVD in the catalog, written as a <c>Disc</c> element.
    /// </summary>
    public sealed class Dvd : Product
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the running time, in minutes.
        /// </summary>
        public int Runtime { get; set; }

        #endregion

    }

}
