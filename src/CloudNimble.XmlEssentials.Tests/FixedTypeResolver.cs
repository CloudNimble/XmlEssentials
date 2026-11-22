using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A resolver that knows one type and answers with the type info of another. Give it the same type twice
    /// for a resolver that knows exactly one type.
    /// </summary>
    public sealed class FixedTypeResolver : IXmlTypeInfoResolver
    {

        #region Fields

        private readonly Type _answer;
        private readonly Type _known;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the resolver.
        /// </summary>
        /// <param name="known">The only type this resolver answers for.</param>
        /// <param name="answer">The type whose type info it returns.</param>
        public FixedTypeResolver(Type known, Type answer)
        {
            _known = known;
            _answer = answer;
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public XmlTypeInfo? GetTypeInfo(Type type, XmlSerializerOptions options)
        {
            return type == _known ? DefaultXmlTypeInfoResolver.Instance.GetTypeInfo(_answer, options) : null;
        }

        #endregion

    }

}
