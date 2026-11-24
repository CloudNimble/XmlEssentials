using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Wrappers
{

    /// <summary>
    /// A member of a polymorphic base type beside a collection of the same base type that is written in a
    /// wrapper element. The derived type names are children of the album for the member and children of the
    /// wrapper for the collection, so the two do not compete for them.
    /// </summary>
    public sealed class ShapeAlbum
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a shape that is written as a child of the album.
        /// </summary>
        public NamedShape? Cover { get; set; }

        /// <summary>
        /// Gets or sets the shapes that are written inside the <c>Pages</c> element.
        /// </summary>
        [XmlArray]
        public List<NamedShape> Pages { get; set; } = [];

        #endregion

    }

}
