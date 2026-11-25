using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// Creates a <see cref="KebabCaseEnumConverter{TEnum}"/> for every enumeration the serializer meets.
    /// </summary>
    public sealed class KebabCaseEnumConverterFactory : XmlConverterFactory
    {

        #region Public Methods

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        /// <inheritdoc />
        public override XmlConverter? CreateConverter(Type typeToConvert, XmlSerializerOptions options)
        {
            Type converterType = typeof(KebabCaseEnumConverter<>).MakeGenericType(typeToConvert);
            return (XmlConverter?)Activator.CreateInstance(converterType);
        }

        #endregion

    }

}
