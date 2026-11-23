using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A caller-defined reference handler whose resolvers name objects <c>k1</c>, <c>k2</c>, and so on.
    /// </summary>
    public sealed class KeyedReferenceHandler : XmlReferenceHandler
    {

        #region Public Methods

        /// <inheritdoc />
        public override XmlReferenceResolver CreateResolver()
        {
            return new KeyedReferenceResolver();
        }

        #endregion

    }

}
