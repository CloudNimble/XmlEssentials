namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Type info for a type that one converter reads and writes whole.
    /// </summary>
    /// <typeparam name="T">The CLR type.</typeparam>
    internal sealed class XmlConverterTypeInfo<T> : XmlTypeInfo<T>
    {

        #region Constructors

        /// <summary>
        /// Creates type info whose document element is named by <see cref="XmlTypeInfo.CreateRootName"/>:
        /// from <see cref="XmlRootAttribute"/> on <typeparamref name="T"/>, or after <typeparamref name="T"/>
        /// under the element naming policy and default namespace of <paramref name="options"/>.
        /// </summary>
        /// <param name="converter">The converter.</param>
        /// <param name="options">The serializer options.</param>
        internal XmlConverterTypeInfo(XmlConverter<T> converter, XmlSerializerOptions options)
            : base(converter, options, CreateRootName(typeof(T), options))
        {
        }

        #endregion

    }

}
