using System;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose constructor rejects an empty name.
    /// </summary>
    public sealed class GuardedName
    {

        #region Public Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="Name">The name. It must not be empty.</param>
        /// <exception cref="ArgumentException"><paramref name="Name"/> is empty.</exception>
        public GuardedName(string Name)
        {
            if (Name.Length == 0)
            {
                throw new ArgumentException("A name is required.", nameof(Name));
            }

            this.Name = Name;
        }

        #endregion

    }

}
