using System;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose constructor takes a value-type member as an interface and keeps the object it was given.
    /// </summary>
    public sealed class KeptArgument
    {

        #region Public Properties

        /// <summary>
        /// Gets the object the constructor was given, exactly as it arrived.
        /// </summary>
        [XmlIgnore]
        public IComparable Kept { get; }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public int Value { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an instance.
        /// </summary>
        /// <param name="Value">The value, as the interface it implements.</param>
        [XmlConstructor]
        public KeptArgument(IComparable Value)
        {
            Kept = Value;
            this.Value = (int)Value;
        }

        #endregion

    }

}
