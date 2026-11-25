using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Represents a value together with the name of the element it is written under. This class cannot be inherited.
    /// </summary>
    /// <example>
    /// <code>
    /// public sealed class Contact
    /// {
    ///     [XmlElement&lt;string&gt;("Home")]
    ///     [XmlElement&lt;string&gt;("Work")]
    ///     public List&lt;XmlNamedValue&gt; Phones { get; set; } = [];
    /// }
    ///
    /// var contact = new Contact { Phones = { new XmlNamedValue("Work", "555-0100"), new XmlNamedValue("Home", "555-0199") } };
    /// string xml = XmlSerializer.Serialize(contact);
    /// // &lt;Contact&gt;&lt;Work&gt;555-0100&lt;/Work&gt;&lt;Home&gt;555-0199&lt;/Home&gt;&lt;/Contact&gt;
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>
    /// A member that carries more than one <see cref="XmlElementAttribute{T}"/> holds a choice of elements, and a
    /// value is written under the element of its type. When two of the elements hold the same type, the type does
    /// not say which element a value is. Declare the member as <see cref="XmlNamedValue"/>, or as a collection of
    /// it, and each value says which element it is by name.
    /// </para>
    /// <para>
    /// <see cref="Name"/> must be the name of one of the member's elements, and <see cref="Value"/> must be of
    /// the type that element holds. A value that is <see langword="null"/> is written as an element marked
    /// <c>xsi:nil</c>.
    /// </para>
    /// </remarks>
    public sealed class XmlNamedValue
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the local name of the element.
        /// </summary>
        /// <value>
        /// The local name of the element the value is written under. The default is an empty string.
        /// </value>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the namespace URI of the element.
        /// </summary>
        /// <value>
        /// The namespace URI the member gives the element, or <see langword="null"/> to find the element by its
        /// local name alone. The default is <see langword="null"/>.
        /// </value>
        /// <remarks>
        /// Set it when two elements of the member have one local name in two namespaces. A value that was read
        /// has the namespace its element was declared with.
        /// </remarks>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the value of the element.
        /// </summary>
        /// <value>
        /// The value, of the type the element holds, or <see langword="null"/>. The default is <see langword="null"/>.
        /// </value>
        public object? Value { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlNamedValue"/> class with no name and no value.
        /// </summary>
        public XmlNamedValue()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlNamedValue"/> class with the specified element name and value.
        /// </summary>
        /// <param name="name">The local name of the element.</param>
        /// <param name="value">The value of the element.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="name"/> is <see langword="null"/>, empty, or white space.
        /// </exception>
        public XmlNamedValue(string name, object? value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name;
            Value = value;
        }

        #endregion

    }

}
