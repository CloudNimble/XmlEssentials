using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Forwards type-info lookup to <see cref="DefaultXmlTypeInfoResolver"/>.
    /// </summary>
    public sealed class PassThroughResolver : IXmlTypeInfoResolver
    {

        #region Public Methods

        /// <inheritdoc />
        public XmlTypeInfo? GetTypeInfo(Type type, XmlSerializerOptions options)
        {
            return DefaultXmlTypeInfoResolver.Instance.GetTypeInfo(type, options);
        }

        #endregion

    }

}
