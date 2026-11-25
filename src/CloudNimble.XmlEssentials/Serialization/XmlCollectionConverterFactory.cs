using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Creates collection and dictionary converters.
    /// </summary>
    [RequiresUnreferencedCode(XmlSerializer.RefMsg), RequiresDynamicCode(XmlSerializer.DynMsg)]
    internal sealed class XmlCollectionConverterFactory : XmlConverterFactory
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlCollectionConverterFactory Instance = new();

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            ArgumentNullException.ThrowIfNull(typeToConvert);
            return XmlCollectionShape.IsDictionary(typeToConvert, out _, out _)
                || XmlCollectionShape.IsCollection(typeToConvert, out _);
        }

        /// <inheritdoc />
        public override XmlConverter? CreateConverter(Type typeToConvert, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(typeToConvert);
            ArgumentNullException.ThrowIfNull(options);
            try
            {
                if (XmlCollectionShape.IsDictionary(typeToConvert, out var keyType, out var valueType))
                {
                    var converterType = typeof(XmlDictionaryConverter<,,>).MakeGenericType(typeToConvert, keyType, valueType);
                    return (XmlConverter)Activator.CreateInstance(converterType, options)!;
                }

                if (!XmlCollectionShape.IsCollection(typeToConvert, out var elementType))
                {
                    return null;
                }

                var collectionConverterType = typeof(XmlCollectionConverter<>).MakeGenericType(typeToConvert);
                return (XmlConverter)Activator.CreateInstance(collectionConverterType, options, elementType)!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }

        #endregion

    }

}
