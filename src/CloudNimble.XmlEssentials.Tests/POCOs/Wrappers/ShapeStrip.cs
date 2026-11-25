using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Wrappers
{

    /// <summary>
    /// A member of a polymorphic base type beside a collection of the same base type that has no wrapper
    /// element. Both write a derived type as a child of the strip, so its name cannot say which one it is.
    /// </summary>
    public sealed class ShapeStrip
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a shape that is written as a child of the strip.
        /// </summary>
        public NamedShape? Cover { get; set; }

        /// <summary>
        /// Gets or sets the shapes that are written as children of the strip.
        /// </summary>
        public List<NamedShape> Frames { get; set; } = [];

        #endregion

    }

}
