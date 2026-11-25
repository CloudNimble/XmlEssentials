using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that deserializes through a marked constructor.
    /// </summary>
    public sealed class CtorBox
    {

        #region Public Properties

        /// <summary>
        /// Gets the payload.
        /// </summary>
        public string Value { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty box.
        /// </summary>
        public CtorBox()
        {
            Value = "";
        }

        /// <summary>
        /// Creates a box with a payload.
        /// </summary>
        /// <param name="Value">The payload.</param>
        [XmlConstructor]
        public CtorBox(string Value)
        {
            this.Value = Value;
        }

        #endregion

    }

}
