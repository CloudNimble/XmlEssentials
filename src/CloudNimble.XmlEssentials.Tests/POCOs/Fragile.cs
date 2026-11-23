using System;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose public parameterless constructor always throws.
    /// </summary>
    public sealed class Fragile
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Always throws.
        /// </summary>
        /// <exception cref="InvalidOperationException">Always.</exception>
        public Fragile()
        {
            throw new InvalidOperationException("Fragile cannot be created.");
        }

        #endregion

    }

}
