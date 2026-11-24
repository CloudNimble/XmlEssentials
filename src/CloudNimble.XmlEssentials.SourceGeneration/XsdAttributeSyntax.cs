using System.Collections.Generic;
using System.Text;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// A mapping attribute of a member generated from a schema, which is written twice: on the property of the
    /// class, and as an object in the metadata of the context.
    /// </summary>
    /// <remarks>
    /// Both come from one description so that they cannot say different things.
    /// </remarks>
    internal sealed class XsdAttributeSyntax
    {

        #region Public Properties

        /// <summary>
        /// Gets the constructor arguments, as C# expressions.
        /// </summary>
        internal List<string> Arguments { get; } = new List<string>();

        /// <summary>
        /// Gets the name of the attribute without the <c>Attribute</c> suffix, such as <c>XmlElement</c>.
        /// </summary>
        internal string Name { get; }

        /// <summary>
        /// Gets the properties the attribute sets, each with its value as a C# expression.
        /// </summary>
        internal List<KeyValuePair<string, string>> Properties { get; } = new List<KeyValuePair<string, string>>();

        /// <summary>
        /// Gets the type argument of a generic attribute, or <see langword="null"/>.
        /// </summary>
        internal string? TypeArgument { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an attribute.
        /// </summary>
        /// <param name="name">The name without the <c>Attribute</c> suffix.</param>
        /// <param name="typeArgument">The type argument of a generic attribute, or <see langword="null"/>.</param>
        internal XsdAttributeSyntax(string name, string? typeArgument = null)
        {
            Name = name;
            TypeArgument = typeArgument;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a constructor argument that is a string.
        /// </summary>
        /// <param name="value">The string.</param>
        /// <returns>This attribute.</returns>
        internal XsdAttributeSyntax Argument(string value)
        {
            Arguments.Add("\"" + ContextEmitter.Escape(value) + "\"");
            return this;
        }

        /// <summary>
        /// Sets the <c>Namespace</c> property, when a namespace was given.
        /// </summary>
        /// <param name="namespaceUri">The namespace, empty for no namespace, or <see langword="null"/> to leave it unset.</param>
        /// <returns>This attribute.</returns>
        internal XsdAttributeSyntax Namespace(string? namespaceUri)
        {
            if (namespaceUri is not null)
            {
                Properties.Add(new KeyValuePair<string, string>("Namespace", "\"" + ContextEmitter.Escape(namespaceUri) + "\""));
            }

            return this;
        }

        /// <summary>
        /// Returns the attribute as it is written on a member, without the brackets.
        /// </summary>
        /// <returns>Such as <c>XmlElement("a", Namespace = "urn:a")</c>.</returns>
        internal string ToAttribute()
        {
            var builder = new StringBuilder(Name);
            if (TypeArgument is not null)
            {
                builder.Append('<').Append(TypeArgument).Append('>');
            }

            if (Arguments.Count == 0 && Properties.Count == 0)
            {
                return builder.ToString();
            }

            builder.Append('(').Append(string.Join(", ", Arguments));
            foreach (var property in Properties)
            {
                builder.Append(builder[builder.Length - 1] == '(' ? "" : ", ").Append(property.Key).Append(" = ").Append(property.Value);
            }

            return builder.Append(')').ToString();
        }

        /// <summary>
        /// Returns the expression that creates the attribute.
        /// </summary>
        /// <returns>Such as <c>new XmlElementAttribute("a") { Namespace = "urn:a" }</c>.</returns>
        internal string ToExpression()
        {
            var builder = new StringBuilder("new ").Append(Name).Append("Attribute");
            if (TypeArgument is not null)
            {
                builder.Append('<').Append(TypeArgument).Append('>');
            }

            builder.Append('(').Append(string.Join(", ", Arguments)).Append(')');
            if (Properties.Count > 0)
            {
                builder.Append(" { ");
                for (var index = 0; index < Properties.Count; index++)
                {
                    builder.Append(index == 0 ? "" : ", ").Append(Properties[index].Key).Append(" = ").Append(Properties[index].Value);
                }

                builder.Append(" }");
            }

            return builder.ToString();
        }

        #endregion

    }

}
