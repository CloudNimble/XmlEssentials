using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A class whose marked constructor has camelCase parameters, next to a constructor with more parameters.
    /// </summary>
    public sealed class CamelMarkedCtor
    {

        #region Public Properties

        /// <summary>
        /// Gets the first value.
        /// </summary>
        public int First { get; }

        /// <summary>
        /// Gets the second value.
        /// </summary>
        public int Second { get; }

        /// <summary>
        /// Gets which constructor created the instance.
        /// </summary>
        [XmlIgnore]
        public string CreatedBy { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the instance from its first value. The second is one more.
        /// </summary>
        /// <param name="first">The first value.</param>
        [XmlConstructor]
        public CamelMarkedCtor(int first)
        {
            First = first;
            Second = first + 1;
            CreatedBy = "marked";
        }

        /// <summary>
        /// Creates the instance from both values.
        /// </summary>
        /// <param name="first">The first value.</param>
        /// <param name="second">The second value.</param>
        public CamelMarkedCtor(int first, int second)
        {
            First = first;
            Second = second;
            CreatedBy = "both";
        }

        #endregion

    }

}
