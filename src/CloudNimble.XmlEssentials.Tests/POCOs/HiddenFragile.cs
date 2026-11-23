using System;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose only constructor is private and always throws.
    /// </summary>
    public sealed class HiddenFragile
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
        private HiddenFragile()
        {
            throw new InvalidOperationException("HiddenFragile cannot be created.");
        }

        #endregion

    }

}
