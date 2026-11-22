namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// <see cref="XmlReferenceHandler"/> that assigns stable ids.
    /// </summary>
    internal sealed class XmlPreserveReferenceHandler : XmlReferenceHandler
    {

        #region Public Methods

        /// <inheritdoc />
        public override XmlReferenceResolver CreateResolver()
        {
            return new XmlPreserveReferenceResolver();
        }

        #endregion

    }

}
