using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// Holds values of a declared type that may be abstract, inside a wrapper element.
    /// </summary>
    /// <typeparam name="T">The declared type.</typeparam>
    public sealed class PolyWrapped<T>
        where T : class
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets what is held.
        /// </summary>
        [XmlArray]
        public List<T> Items { get; set; } = [];

        #endregion

    }

}
