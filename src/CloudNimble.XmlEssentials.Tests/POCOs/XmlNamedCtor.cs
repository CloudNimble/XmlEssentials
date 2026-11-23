using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A class whose constructor parameter is named like the XML name of a member, in another case.
    /// </summary>
    public sealed class XmlNamedCtor
    {

        #region Public Properties

        /// <summary>
        /// Gets the heading, written as <c>title</c>.
        /// </summary>
        [XmlElement("title")]
        public string Heading { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="TITLE">The heading.</param>
        public XmlNamedCtor(string TITLE)
        {
            Heading = TITLE;
        }

        #endregion

    }

}
