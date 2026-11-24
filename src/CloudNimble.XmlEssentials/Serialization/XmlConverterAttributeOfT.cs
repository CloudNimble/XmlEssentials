using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the converter type to use when placed on a property, field, or type. This class cannot be inherited.
    /// </summary>
    /// <typeparam name="TConverter">
    /// The type of the converter. It derives from <see cref="XmlConverter"/> and has a public parameterless constructor.
    /// </typeparam>
    /// <example>
    /// <code>
    /// [XmlConverter&lt;MoneyConverter&gt;]
    /// public readonly record struct Money(decimal Amount, string Currency);
    /// </code>
    /// </example>
    /// <remarks>
    /// On a type, the converter reads and writes every value of the type, unless <see cref="XmlSerializerOptions.Converters"/>
    /// holds a converter for it. On a property or field, the converter reads and writes that member only, and comes before
    /// both. It handles the whole value of the member: a collection is not taken apart into items, the member may be an
    /// attribute or text, and a converter for <c>T</c> serves a member of type <c>T?</c>. A source-generated context reads the
    /// attribute on a type, and reports it on a member as the error XMLXE009. The converter is a type argument, so the
    /// compiler checks that it is a converter and can be created, and no reflection is needed to create it.
    /// </remarks>
    public sealed class XmlConverterAttribute<TConverter> : XmlConverterAttribute
        where TConverter : XmlConverter, new()
    {

        #region Public Properties

        /// <inheritdoc />
        public override Type ConverterType => typeof(TConverter);

        #endregion

        #region Private Methods

        /// <inheritdoc />
        internal override XmlConverter CreateConverter()
        {
            return new TConverter();
        }

        #endregion

    }

}
