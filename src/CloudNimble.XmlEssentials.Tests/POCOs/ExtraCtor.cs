namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a constructor whose parameter is not a serialized member.
    /// </summary>
    public sealed class ExtraCtor
    {

        #region Public Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty instance.
        /// </summary>
        public ExtraCtor()
        {
        }

        /// <summary>
        /// Creates an instance from a value that is not a member.
        /// </summary>
        /// <param name="unused">A value that is not serialized.</param>
        public ExtraCtor(int unused)
        {
        }

        #endregion

    }

}
