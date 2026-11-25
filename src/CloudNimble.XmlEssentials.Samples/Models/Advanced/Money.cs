namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// An amount of money in one currency.
    /// </summary>
    public readonly struct Money
    {

        #region Public Properties

        /// <summary>
        /// Gets the amount.
        /// </summary>
        public decimal Amount { get; }

        /// <summary>
        /// Gets the ISO 4217 code of the currency.
        /// </summary>
        public string Currency { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Money"/> struct.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="currency">The ISO 4217 code of the currency.</param>
        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        #endregion

    }

}
