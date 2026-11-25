using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds a member and a collection in a wrapper, both declared as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The declared type of every value.</typeparam>
    public sealed class Held<T>
    {

        #region Public Properties

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
