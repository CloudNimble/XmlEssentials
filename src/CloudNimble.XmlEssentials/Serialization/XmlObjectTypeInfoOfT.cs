using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Type info for a CLR type mapped as an object with child elements.
    /// </summary>
    /// <typeparam name="T">The CLR type.</typeparam>
    internal sealed class XmlObjectTypeInfo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T> : XmlTypeInfo<T>
    {

        #region Fields

        private readonly XmlObjectBinding _binding;
        private readonly XmlPropertyInfo[] _properties;

        #endregion

        #region Public Properties

        /// <inheritdoc />
        public override Func<object>? CreateObject => _binding.CreateObject;

        /// <inheritdoc />
        public override IList<XmlPropertyInfo> Properties => _properties;

        /// <summary>
        /// Gets the bound members.
        /// </summary>
        internal XmlObjectBinding Binding => _binding;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates type info, caches it on <paramref name="options"/>, then binds members.
        /// </summary>
        /// <param name="converter">The object converter.</param>
        /// <param name="options">The serializer options.</param>
        internal XmlObjectTypeInfo(XmlConverter<T> converter, XmlSerializerOptions options)
            : base(converter, options, CreateRootName(options))
        {
            options.CacheTypeInfo(this);
            try
            {
                _binding = XmlObjectBinder.Bind<T>(options);
            }
            catch
            {
                options.RemoveTypeInfo(typeof(T));
                throw;
            }
            _properties = new XmlPropertyInfo[_binding.Properties.Length];
            for (var i = 0; i < _properties.Length; i++)
            {
                _properties[i] = _binding.Properties[i].Info;
            }
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        internal override T? ReadRoot(ref Utf8XmlReader reader)
        {
            XmlConverterHelpers.MoveToStartElement(ref reader);
            var polymorphism = _binding.Polymorphism;
            if (polymorphism.Placement is XmlDiscriminatorPlacement.ElementName)
            {
                foreach (var derived in polymorphism.Derived)
                {
                    if (reader.LocalNameEquals(derived.Utf8Name))
                    {
                        return (T?)derived.Info.ReadElementAsObject(ref reader);
                    }
                }
            }
            else if (XmlConverterHelpers.ReadDiscriminator(ref reader, polymorphism.Placement, polymorphism.Utf8DiscriminatorAttributeName) is { Length: > 0 } discriminator)
            {
                if (polymorphism.FindByDiscriminator(discriminator) is { } derived)
                {
                    CheckRoot(ref reader);
                    return (T?)derived.Info.ReadElementAsObject(ref reader);
                }

                if (polymorphism.UnknownDerivedTypeHandling is XmlUnknownDerivedTypeHandling.FailSerialization)
                {
                    throw new XmlException($"Unknown type discriminator '{discriminator}'.");
                }
            }

            CheckRoot(ref reader);
            return ReadElement(ref reader);
        }

        /// <inheritdoc />
        internal override void WriteRoot(Utf8XmlWriter writer, T value)
        {
            if (value is null)
            {
                WriteNullRoot(writer);
                return;
            }

            var polymorphism = _binding.Polymorphism;
            if (polymorphism.Derived.Length > 0 && polymorphism.TryGetDerivedWrite(value.GetType()) is { } derived)
            {
                if (polymorphism.Placement is XmlDiscriminatorPlacement.ElementName)
                {
                    writer.WriteStartElement(derived.Utf8Name);
                }
                else
                {
                    WriteStartRoot(writer);
                    XmlConverterHelpers.WriteDiscriminator(writer, polymorphism.Placement, polymorphism.Utf8DiscriminatorAttributeName, derived.Discriminator);
                }

                derived.Info.WriteContentAsObject(writer, value);
                writer.WriteEndElement();
                return;
            }

            WriteStartRoot(writer);
            Converter.Write(writer, value, Options);
            writer.WriteEndElement();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Builds the document element name from <see cref="XmlRootAttribute"/> or the type name.
        /// </summary>
        /// <param name="options">The serializer options.</param>
        /// <returns>The root name.</returns>
        internal static XmlName CreateRootName(XmlSerializerOptions options)
        {
            return CreateRootName(typeof(T), options);
        }

        #endregion

    }

}
