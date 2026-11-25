// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the tests.
#if NET11_0_OR_GREATER
using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Closed
{

    /// <summary>
    /// Holds closed shapes as the items of a collection in a wrapper.
    /// </summary>
    public sealed class ClosedShapeList
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the shapes.
        /// </summary>
        [XmlArray]
        public List<ClosedShape> Shapes { get; set; } = [];

        #endregion

    }

}
#endif
