using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// One alternative of a member that holds a choice of elements: the element name that stands for a type.
    /// </summary>
    /// <remarks>
    /// A member has a choice when it carries more than one <see cref="XmlElementAttribute"/>, or a collection in a
    /// wrapper carries more than one <see cref="XmlArrayItemAttribute"/>, each naming its type.
    /// </remarks>
    internal sealed class XmlElementChoice
    {

        #region Public Properties

        /// <summary>
        /// Gets the element name, or <see langword="null"/> when the element is named for the type.
        /// </summary>
        internal string? ElementName { get; }

        /// <summary>
        /// Gets the namespace of the element, or <see langword="null"/> when none was given.
        /// </summary>
        internal string? Namespace { get; }

        /// <summary>
        /// Gets the type the element holds.
        /// </summary>
        internal Type Type { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an alternative.
        /// </summary>
        /// <param name="elementName">The element name, or <see langword="null"/> or empty to name it for the type.</param>
        /// <param name="namespaceUri">The namespace of the element, or <see langword="null"/>.</param>
        /// <param name="type">The type the element holds.</param>
        internal XmlElementChoice(string? elementName, string? namespaceUri, Type type)
        {
            ElementName = elementName is { Length: > 0 } ? elementName : null;
            Namespace = namespaceUri;
            Type = type;
        }

        #endregion

    }

}