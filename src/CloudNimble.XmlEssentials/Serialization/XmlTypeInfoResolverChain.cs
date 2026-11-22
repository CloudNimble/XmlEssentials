using System;
using System.Collections.Generic;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// The <see cref="XmlSerializerOptions.TypeInfoResolverChain"/> list: a view of
    /// <see cref="XmlSerializerOptions.TypeInfoResolver"/> as an ordered list of resolvers, and itself the
    /// resolver that asks each of them in turn.
    /// </summary>
    internal sealed class XmlTypeInfoResolverChain : XmlConfigurationList<IXmlTypeInfoResolver>, IXmlTypeInfoResolver
    {

        #region Constructors

        /// <summary>
        /// Creates the chain from the resolver the options currently use.
        /// </summary>
        /// <param name="options">The owner.</param>
        /// <param name="resolver">The current resolver.</param>
        internal XmlTypeInfoResolverChain(XmlSerializerOptions options, IXmlTypeInfoResolver? resolver)
            : base(options)
        {
            Reset(resolver);
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public XmlTypeInfo? GetTypeInfo(Type type, XmlSerializerOptions options)
        {
            foreach (var resolver in this)
            {
                if (resolver.GetTypeInfo(type, options) is { } typeInfo)
                {
                    return typeInfo;
                }
            }

            return null;
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        private protected override void OnChanged()
        {
            Options.SetTypeInfoResolverChain(this);
        }

        /// <summary>
        /// Makes the list show <paramref name="resolver"/>: nothing for <see langword="null"/>, the items of
        /// another chain, or the resolver as the single item.
        /// </summary>
        /// <param name="resolver">The resolver the options now use.</param>
        internal void Reset(IXmlTypeInfoResolver? resolver)
        {
            if (ReferenceEquals(resolver, this))
            {
                return;
            }

            ReplaceAll(resolver as XmlTypeInfoResolverChain ?? (IEnumerable<IXmlTypeInfoResolver>)(resolver is null ? [] : [resolver]));
        }

        #endregion

    }

}
