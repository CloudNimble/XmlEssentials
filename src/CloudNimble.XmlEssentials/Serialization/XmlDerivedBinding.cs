using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// One derived type that can appear where a base member is expected.
    /// </summary>
    internal readonly struct XmlDerivedBinding
    {

        #region Public Properties

        /// <summary>
        /// Gets the type info of <see cref="Type"/>.
        /// </summary>
        internal XmlTypeInfo Info { get; }

        /// <summary>
        /// Gets the derived CLR type.
        /// </summary>
        internal Type Type { get; }

        /// <summary>
        /// Gets the discriminator as written in an attribute or <c>xsi:type</c> value.
        /// </summary>
        internal string Discriminator { get; }

        /// <summary>
        /// Gets the UTF-8 discriminator element name.
        /// </summary>
        internal byte[] Utf8Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the binding.
        /// </summary>
        /// <param name="utf8Name">The discriminator local name.</param>
        /// <param name="type">The derived CLR type.</param>
        /// <param name="info">The type info.</param>
        /// <param name="discriminator">The discriminator string.</param>
        internal XmlDerivedBinding(byte[] utf8Name, Type type, XmlTypeInfo info, string discriminator)
        {
            Utf8Name = utf8Name;
            Type = type;
            Info = info;
            Discriminator = discriminator;
        }

        #endregion

    }

}
