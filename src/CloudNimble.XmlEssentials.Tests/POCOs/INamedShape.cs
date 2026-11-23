using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An interface that is the declared type of NamedCircle and NamedSquare values.
    /// </summary>
    [XmlDerivedType<NamedCircle>]
    [XmlDerivedType<NamedSquare>]
    public interface INamedShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        string? Label { get; set; }

        #endregion

    }

}
