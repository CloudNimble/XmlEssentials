using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Provides information about a constructor parameter that is required for serialization and deserialization.
    /// This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// Source-generated contexts create instances of this class. It is analogous to <c>JsonParameterInfoValues</c> in
    /// <c>System.Text.Json</c>.
    /// </remarks>
    public sealed class XmlParameterInfoValues
    {

        #region Public Properties

        /// <summary>
        /// Gets or initializes the default value of the parameter.
        /// </summary>
        /// <value>
        /// The value the parameter takes when the document has none for it. It is used only when
        /// <see cref="HasDefaultValue"/> is <see langword="true"/>.
        /// </value>
        public object? DefaultValue { get; init; }

        /// <summary>
        /// Gets or initializes a value that specifies whether a default value was specified for the parameter.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the parameter has a default value; otherwise, <see langword="false"/>.
        /// </value>
        public bool HasDefaultValue { get; init; }

        /// <summary>
        /// Gets or initializes a value that indicates whether the parameter accepts <see langword="null"/>.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the nullable annotations of the parameter allow <see langword="null"/>, or say
        /// nothing; otherwise, <see langword="false"/>.
        /// </value>
        /// <remarks>
        /// The value is read when <see cref="XmlSerializerOptions.RespectNullableAnnotations"/> is <see langword="true"/>.
        /// </remarks>
        public bool IsNullable { get; init; }

        /// <summary>
        /// Gets or initializes the name of the parameter.
        /// </summary>
        /// <value>
        /// The name, which is matched to a member of the type without regard to case.
        /// </value>
        public string Name { get; init; } = "";

        /// <summary>
        /// Gets or initializes the type of the parameter.
        /// </summary>
        /// <value>
        /// The type of the parameter.
        /// </value>
        public Type ParameterType { get; init; } = typeof(object);

        /// <summary>
        /// Gets or initializes the zero-based position of the parameter in the formal parameter list.
        /// </summary>
        /// <value>
        /// The position. The arguments are passed to the object creator in this order.
        /// </value>
        public int Position { get; init; }

        #endregion

    }

}
