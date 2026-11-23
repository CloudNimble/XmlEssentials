namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// A price quote. <see cref="QuoteXmlContext"/> lists this type.
    /// </summary>
    public sealed class Quote
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the quote number.
        /// </summary>
        public string? Number { get; set; }

        /// <summary>
        /// Gets or sets the quoted price.
        /// </summary>
        public Money Price { get; set; }

        #endregion

    }

}
