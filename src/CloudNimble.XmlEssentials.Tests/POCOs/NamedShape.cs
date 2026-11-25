using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An abstract class that is the declared type of NamedCircle and NamedSquare values.
    /// </summary>
    [XmlDerivedType<NamedCircle>]
    [XmlDerivedType<NamedSquare>]
    public abstract class NamedShape : INamedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string? Label { get; set; }

        #endregion

    }

}
