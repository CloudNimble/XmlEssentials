using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type created through a constructor that also has a member set afterwards and a member that is only read.
    /// </summary>
    public sealed class CtorExtra
    {

        #region Public Properties

        /// <summary>
        /// Gets the length of <see cref="Value"/>. It is written but cannot be set.
        /// </summary>
        public int Length => Value.Length;

        /// <summary>
        /// Gets or sets a note, set after construction.
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Gets the value, set by the constructor.
        /// </summary>
        public string Value { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="Value">The value.</param>
        [XmlConstructor]
        public CtorExtra(string Value)
        {
            this.Value = Value;
        }

        #endregion

    }

}
