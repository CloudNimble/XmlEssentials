using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Advanced
{

    /// <summary>
    /// Converts one enumeration to and from kebab-case names such as <c>ready-to-ship</c>.
    /// </summary>
    /// <typeparam name="TEnum">The enumeration to convert.</typeparam>
    public sealed class KebabCaseEnumConverter<TEnum> : XmlConverter<TEnum>
        where TEnum : struct, Enum
    {

        #region Public Methods

        /// <inheritdoc />
        public override TEnum Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            TEnum value = default;
            if (reader.IsEmptyElement)
            {
                return value;
            }

            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                if (reader.TokenType is XmlTokenType.Text)
                {
                    value = Parse(reader.GetString()!);
                }
            }

            return value;
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, TEnum value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(XmlNamingPolicy.KebabCaseLower.ConvertName(value.ToString()));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Finds the member of <typeparamref name="TEnum"/> whose kebab-case name is <paramref name="text"/>.
        /// </summary>
        /// <param name="text">The text read from the document.</param>
        /// <returns>The matching member.</returns>
        /// <exception cref="XmlException">No member has that name.</exception>
        private static TEnum Parse(string text)
        {
            foreach (TEnum candidate in Enum.GetValues<TEnum>())
            {
                if (XmlNamingPolicy.KebabCaseLower.ConvertName(candidate.ToString()) == text)
                {
                    return candidate;
                }
            }

            throw new XmlException($"'{text}' is not a value of {typeof(TEnum).Name}.");
        }

        #endregion

    }

}
