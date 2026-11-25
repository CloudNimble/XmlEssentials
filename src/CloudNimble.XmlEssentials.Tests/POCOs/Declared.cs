using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds values declared as <typeparamref name="T"/> in each place a value can be: a member, an item of a
    /// collection in a wrapper, and an item of a collection with no wrapper.
    /// </summary>
    /// <typeparam name="T">The declared type of every value.</typeparam>
    public sealed class Declared<T>
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the items written with no wrapper.
        /// </summary>
        public List<T>? Flat { get; set; }

        /// <summary>
        /// Gets or sets the member.
        /// </summary>
        public T? One { get; set; }

        /// <summary>
        /// Gets or sets the items written in a wrapper.
        /// </summary>
        [XmlArray("Wrapped")]
        public List<T>? Wrapped { get; set; }

        #endregion

    }

}
