using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A caller-defined reference handler whose resolvers give objects no identifier: they only say whether an
    /// object was seen before.
    /// </summary>
    public sealed class NamelessReferenceHandler : XmlReferenceHandler
    {

        #region Public Methods

        /// <inheritdoc />
        public override XmlReferenceResolver CreateResolver()
        {
            return new NamelessReferenceResolver();
        }

        #endregion

    }

}
