namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// A product for sale, with a <see cref="Price"/> that has simple content.
    /// </summary>
    public sealed class Listing
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the product name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the price.
        /// </summary>
        public Price? Price { get; set; }

        #endregion

    }

}
