using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Samples.Models.Migration
{

    /// <summary>
    /// A hotel reservation: an internal record with a constructor, a required member, an init-only member, and a dictionary.
    /// </summary>
    /// <param name="Guest">The guest's name.</param>
    /// <param name="Nights">The number of nights.</param>
    internal sealed record Reservation(string Guest, int Nights)
    {

        #region Public Properties

        /// <summary>
        /// Gets or initializes the charges, keyed by what each one is for.
        /// </summary>
        public Dictionary<string, decimal> Charges { get; init; } = [];

        /// <summary>
        /// Gets or initializes the room number.
        /// </summary>
        public required string Room { get; init; }

        #endregion

    }

}
