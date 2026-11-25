using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with two constructors marked <see cref="XmlConstructorAttribute"/>.
    /// </summary>
    public sealed class TwoMarkedCtors
    {

        #region Public Properties

        /// <summary>
        /// Gets the payload.
        /// </summary>
        public int X { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an empty instance.
        /// </summary>
        [XmlConstructor]
        public TwoMarkedCtors()
        {
        }

        /// <summary>
        /// Creates an instance with a payload.
        /// </summary>
        /// <param name="X">The payload.</param>
        [XmlConstructor]
        public TwoMarkedCtors(int X)
        {
            this.X = X;
        }

        #endregion

    }

}
