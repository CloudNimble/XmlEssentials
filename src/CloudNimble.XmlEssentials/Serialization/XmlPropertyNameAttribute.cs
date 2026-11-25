using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Specifies the local name of the XML element or attribute that a property or field is serialized as. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// The name overrides any naming policy, and takes precedence over the name set by <see cref="XmlElementAttribute"/>,
    /// <see cref="XmlAttributeAttribute"/>, or <see cref="XmlArrayAttribute"/> on the same member. This attribute is
    /// analogous to <c>JsonPropertyNameAttribute</c> in <c>System.Text.Json</c>.
    /// <para>
    /// On a member of an enumeration, the name is the text the member is written as and read from, as
    /// <c>JsonStringEnumMemberNameAttribute</c> is in <c>System.Text.Json</c>. It replaces the C# name and is
    /// matched exactly, while a C# name is matched without regard to case. A name cannot start or end with
    /// white space, and the name of a <see cref="FlagsAttribute"/> member cannot contain any.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlPropertyNameAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets the local name of the XML element or attribute.
        /// </summary>
        /// <value>
        /// The local name of the XML element or attribute.
        /// </value>
        public string Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlPropertyNameAttribute"/> class with the specified name.
        /// </summary>
        /// <param name="name">The local name of the XML element or attribute.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="name"/> is empty or consists only of white-space characters.
        /// </exception>
        public XmlPropertyNameAttribute(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name;
        }

        #endregion

    }

}
