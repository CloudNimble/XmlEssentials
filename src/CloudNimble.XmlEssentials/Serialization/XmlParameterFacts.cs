using System;
using System.Reflection;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// What the serializer has to know about one parameter of the constructor it creates an object through.
    /// </summary>
    internal sealed class XmlParameterFacts
    {

        #region Fields

        /// <summary>
        /// The value the parameter takes when the document has none for it. Meaningful when <see cref="HasDefaultValue"/> is set.
        /// </summary>
        internal readonly object? DefaultValue;

        /// <summary>
        /// Whether the parameter declares a default value.
        /// </summary>
        internal readonly bool HasDefaultValue;

        /// <summary>
        /// Whether the parameter belongs to a member that is never serialized, so that no document can supply it
        /// and it takes the default of its type.
        /// </summary>
        internal readonly bool IsIgnoredMember;

        /// <summary>
        /// The name of the parameter, or null when it has none.
        /// </summary>
        internal readonly string? Name;

        /// <summary>
        /// The reflected parameter, or null when the facts were not filled by reflection.
        /// </summary>
        internal ParameterInfo? Parameter;

        /// <summary>
        /// The type of the parameter.
        /// </summary>
        internal readonly Type ParameterType;

        /// <summary>
        /// Whether the nullable annotations of the parameter say it does not accept null.
        /// </summary>
        internal bool RefusesNull;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates the facts of a parameter.
        /// </summary>
        /// <param name="name">The name of the parameter, or null.</param>
        /// <param name="parameterType">The type of the parameter.</param>
        /// <param name="hasDefaultValue">Whether the parameter declares a default value.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="isIgnoredMember">Whether the parameter belongs to a member that is never serialized.</param>
        internal XmlParameterFacts(string? name, Type parameterType, bool hasDefaultValue, object? defaultValue, bool isIgnoredMember)
        {
            Name = name;
            ParameterType = parameterType;
            HasDefaultValue = hasDefaultValue;
            DefaultValue = defaultValue;
            IsIgnoredMember = isIgnoredMember;
        }

        #endregion

    }

}
