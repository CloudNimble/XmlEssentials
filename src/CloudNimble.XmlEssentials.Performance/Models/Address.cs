
namespace CloudNimble.XmlEssentials.Performance.Models
{

    /// <summary>
    /// A postal address, nested in <see cref="Customer"/>.
    /// </summary>
    public sealed class Address
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        public string? City { get; set; }

        /// <summary>
        /// Gets or sets the postal code.
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// Gets or sets the street.
        /// </summary>
        public string? Street { get; set; }

        #endregion

    }

}
