using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A hand-written context that reuses <see cref="DefaultXmlTypeInfoResolver"/>.
    /// </summary>
    public sealed class PrimitiveXmlContext : XmlSerializerContext
    {

        #region Constructors

        /// <summary>
        /// Creates a context with a new mutable options instance.
        /// </summary>
        public PrimitiveXmlContext()
            : base(null)
        {
        }

        /// <summary>
        /// Creates a context with <paramref name="options"/>.
        /// </summary>
        /// <param name="options">The options.</param>
        public PrimitiveXmlContext(XmlSerializerOptions options)
            : base(options)
        {
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override XmlTypeInfo? GetTypeInfo(Type type)
        {
            return DefaultXmlTypeInfoResolver.Instance.GetTypeInfo(type, Options);
        }

        #endregion

    }

}
