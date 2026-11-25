using System;
using CloudNimble.Text.Xml.Nodes;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Converts a value declared as <see cref="object"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is written as the type it has when it is written. A document does not record that type, so
    /// the element is read back as XML: an <see cref="XmlElement"/>, or an <see cref="XmlNode"/> when
    /// <see cref="XmlSerializerOptions.UnknownTypeHandling"/> says so.
    /// </para>
    /// <para>
    /// The element that is read is the one that names the value: the member's element, the item's or the
    /// entry's. So an <see cref="XmlElement"/> or an <see cref="XmlElementNode"/> is written as its attributes and
    /// its children inside that element, under the name the member, the item or the entry has, and what was read
    /// is written back as it was.
    /// </para>
    /// </remarks>
    internal sealed class XmlRuntimeObjectConverter : XmlConverter<object>
    {

        #region Fields

        /// <summary>
        /// The shared instance.
        /// </summary>
        internal static readonly XmlRuntimeObjectConverter Instance = new();

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public override object? Read(ref Utf8XmlReader reader, Type typeToConvert, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);
            return options.UnknownTypeHandling is XmlUnknownTypeHandling.XmlNode
                ? XmlNodeFragmentConverter<XmlNode>.Instance.ReadValue(ref reader)
                : XmlElementFragmentConverter.Instance.ReadValue(ref reader);
        }

        /// <inheritdoc />
        public override void Write(Utf8XmlWriter writer, object value, XmlSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(value);
            ArgumentNullException.ThrowIfNull(options);

            // An element is what a value declared as object is read as, and the element that was read is the one
            // that names the value. So an element fills the element that is open, and what was read is written back.
            if (value is XmlElement element)
            {
                if (element.ValueKind is not XmlValueKind.Undefined)
                {
                    element.WriteContentTo(writer);
                }

                return;
            }

            if (value is XmlElementNode node)
            {
                node.WriteContentTo(writer);
                return;
            }

            var type = value.GetType();
            if (type == typeof(object))
            {
                // An instance of object itself has nothing to write, and asking for its type info would come back here.
                return;
            }

            options.GetTypeInfo(type).WriteContentAsObject(writer, value);
        }

        #endregion

        #region Private Methods

        /// <inheritdoc />
        private protected override XmlTypeInfo CreateTypeInfo(XmlSerializerOptions options)
        {
            return new XmlRuntimeObjectTypeInfo(this, options);
        }

        #endregion

    }

}
