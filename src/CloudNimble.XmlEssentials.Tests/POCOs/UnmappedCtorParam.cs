using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose marked constructor takes a parameter that is not a serialized member.
    /// </summary>
    public sealed class UnmappedCtorParam
    {

        #region Public Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string? Name { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance from a value that is not a member.
        /// </summary>
        /// <param name="unused">A value that is not serialized.</param>
        [XmlConstructor]
        public UnmappedCtorParam(int unused)
        {
        }

        #endregion

    }

}
