namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// A <see cref="Product"/> that is a DVD.
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
