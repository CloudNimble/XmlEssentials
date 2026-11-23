using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An abstract class that writes a derived type it does not declare as itself.
    /// </summary>
    [XmlPolymorphic(UnknownDerivedTypeHandling = XmlUnknownDerivedTypeHandling.FallBackToBaseType)]
    [XmlDerivedType<LooseCircle>]
    public abstract class LooseShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string? Label { get; set; }

        #endregion

    }

}
