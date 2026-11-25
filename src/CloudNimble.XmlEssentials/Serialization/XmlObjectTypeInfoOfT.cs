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
    internal sealed class XmlObjectTypeInfo<T> : XmlTypeInfo<T>
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

        /// <inheritdoc />
        /// <remarks>
        /// The built-in object converter is what makes an object type info, and only what it reads can stop part
        /// way: a type info given any other converter has no binding to offer. An abstract type is never created,
        /// and a type info with no member metadata cannot read at all.
        /// </remarks>
        internal override XmlObjectBinding? ResumableBinding => Converter is XmlObjectConverter<T> && !typeof(T).IsAbstract && !_binding.NoMetadata ? _binding : null;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates type info, caches it on <paramref name="options"/>, then binds members.
        /// </summary>
        /// <param name="converter">The object converter.</param>
        /// <param name="options">The serializer options.</param>
        /// <param name="bind">Builds the binding of the type.</param>
        internal XmlObjectTypeInfo(XmlConverter<T> converter, XmlSerializerOptions options, Func<XmlSerializerOptions, XmlObjectBinding> bind)
            : base(converter, options, CreateRootName(options))
        {
            options.CacheTypeInfo(this);
            try
            {
                _binding = bind(options);
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
                foreach (var derived in polymorphism.Known)
                {
                    if (derived.Matches(ref reader, Options.PropertyNameCaseInsensitive))
                    {
                        return (T?)polymorphism.ReadDerived(ref reader, derived);
                    }
                }
            }
            else if (polymorphism.ReadDiscriminator(ref reader, out var unknown) is { } derived)
            {
                CheckRoot(ref reader);
                return (T?)polymorphism.ReadDerived(ref reader, derived);
            }
            else if (unknown is not null && polymorphism.UnknownDerivedTypeHandling is XmlUnknownDerivedTypeHandling.FailSerialization)
            {
                throw XmlException.Create($"Unknown type discriminator '{unknown}'.");
            }

            CheckRoot(ref reader);
            return ReadElement(ref reader);
        }

        /// <inheritdoc />
        /// <remarks>
        /// A value of a stream is the type its element names, as a value of a member or an item of a collection is.
        /// </remarks>
        internal override T? ReadStreamedElement(ref Utf8XmlReader reader)
        {
            var polymorphism = _binding.Polymorphism;
            return polymorphism.IsActive ? (T?)polymorphism.ReadValue(ref reader, this) : base.ReadStreamedElement(ref reader);
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
                    // The document element has the derived type's name, and the declarations of this type.
                    if (!TryWriteStartDocumentElement(writer, derived.Utf8Name, derived.Utf8NamespaceUri))
                    {
                        derived.WriteStart(writer);
                    }
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
