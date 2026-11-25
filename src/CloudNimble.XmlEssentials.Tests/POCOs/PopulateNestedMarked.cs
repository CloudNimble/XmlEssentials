using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type that marks one member to be populated; the mark does not reach the members of the object it holds.
    /// </summary>
    public sealed class PopulateNestedMarked
    {

        #region Public Properties

        /// <summary>
        /// Gets the inner object.
        /// </summary>
        [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
        public PopulateInner Inner { get; } = new();

        #endregion

    }

}
