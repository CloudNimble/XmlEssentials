namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// <see cref="XmlReferenceHandler"/> that reports already-seen objects without assigning ids.
    /// </summary>
    internal sealed class XmlIgnoreCyclesReferenceHandler : XmlReferenceHandler
    {

        #region Public Methods

        /// <inheritdoc />
        public override XmlReferenceResolver CreateResolver()
        {
            return new XmlIgnoreCyclesReferenceResolver();
        }

        #endregion

    }

}
