using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using CloudNimble.XmlEssentials.Tests.POCOs;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// A converter that writes a <see cref="Person"/> as its name only, and reads the name back.
    /// </summary>
    public sealed class PersonNameConverter : XmlConverter<Person>
    {

        #region Public Methods

        /// <inheritdoc />
        public override Person? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            var person = new Person();
            while (reader.Read() && reader.TokenType is not XmlTokenType.EndElement)
            {
                person.Name = reader.GetString();
            }

            return person;
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, Person value, XmlSerializerOptions options)
        {
            writer.WriteStringValue(value.Name);
        }

        #endregion

    }

}
