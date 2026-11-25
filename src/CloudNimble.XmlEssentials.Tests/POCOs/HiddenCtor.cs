using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose only constructor is non-public and marked <see cref="XmlConstructorAttribute"/>.
    /// </summary>
    public sealed class HiddenCtor
    {

        #region Public Properties

        /// <summary>
        /// Gets the payload.
        /// </summary>
        public string Value { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="Value">The payload.</param>
        [XmlConstructor]
        private HiddenCtor(string Value)
        {
            this.Value = Value;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates an instance for tests that serialize a known value.
        /// </summary>
        /// <param name="value">The payload.</param>
        /// <returns>The instance.</returns>
        public static HiddenCtor Create(string value)
        {
            return new HiddenCtor(value);
        }

        #endregion

    }

}
