namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A payment made by card.
    /// </summary>
    public sealed class CardPayment : Payment
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the last four digits of the card number.
        /// </summary>
        public string? LastFour { get; set; }

        #endregion

    }

}
