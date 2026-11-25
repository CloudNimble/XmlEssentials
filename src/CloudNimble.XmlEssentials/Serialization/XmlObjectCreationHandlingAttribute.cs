using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Determines how a member that already holds a value is handled when it is deserialized. This class cannot be inherited.
    /// </summary>
    /// <example>
    /// <code>
    /// public sealed class Playlist
    /// {
    ///     [XmlObjectCreationHandling(XmlObjectCreationHandling.Populate)]
    ///     public List&lt;string&gt; Tracks { get; } = [];
    /// }
    /// </code>
    /// </example>
    /// <remarks>
    /// <para>
    /// On a property or field, the attribute takes precedence over
    /// <see cref="XmlSerializerOptions.PreferredObjectCreationHandling"/> for that member. On a type, it applies to
    /// every member of the type that can be populated.
    /// </para>
    /// <para>
    /// <see cref="XmlObjectCreationHandling.Populate"/> on a member that cannot be populated throws an
    /// <see cref="InvalidOperationException"/> when the type is first used: an array, an immutable collection, a
    /// value with a custom converter, a polymorphic type, or a value type with no setter. On a type that is created
    /// through a constructor with parameters it throws a <see cref="NotSupportedException"/>.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class XmlObjectCreationHandlingAttribute : Attribute
    {

        #region Public Properties

        /// <summary>
        /// Gets the handling that applies to the member, or to the members of the type.
        /// </summary>
        /// <value>
        /// One of the enumeration values that specifies whether an existing value is replaced or populated.
        /// </value>
        public XmlObjectCreationHandling Handling { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlObjectCreationHandlingAttribute"/> class.
        /// </summary>
        /// <param name="handling">Whether an existing value is replaced or populated.</param>
        public XmlObjectCreationHandlingAttribute(XmlObjectCreationHandling handling)
        {
            Handling = handling;
        }

        #endregion

    }

}
