using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Creates <see cref="XmlObjectConverter{T}"/> for concrete classes and structs.
    /// </summary>
    internal sealed class XmlObjectConverterFactory : XmlConverterFactory
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlObjectConverterFactory Instance = new();

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert)
        {
            ArgumentNullException.ThrowIfNull(typeToConvert);
            return XmlObjectBinder.IsObjectType(typeToConvert);
        }

        /// <inheritdoc />
        [UnconditionalSuppressMessage("Trimming", "IL2026")]
        [UnconditionalSuppressMessage("Trimming", "IL2071")]
        [UnconditionalSuppressMessage("AOT", "IL3050")]
        public override XmlConverter? CreateConverter(Type typeToConvert, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(typeToConvert);
            ArgumentNullException.ThrowIfNull(options);
            if (!XmlObjectBinder.IsObjectType(typeToConvert))
            {
                return null;
            }

            var converterType = typeof(XmlObjectConverter<>).MakeGenericType(typeToConvert);
            try
            {
                return (XmlConverter)Activator.CreateInstance(converterType, options)!;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                throw ex.InnerException;
            }
        }

        #endregion

    }

}
