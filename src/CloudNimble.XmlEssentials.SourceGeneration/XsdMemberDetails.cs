using System;
using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.SourceGeneration
{

    /// <summary>
    /// What a member generated from a schema needs beyond what a member of a hand-written type has.
    /// </summary>
    internal sealed class XsdMemberDetails : IEquatable<XsdMemberDetails>
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the elements the member may hold, each standing for a type, or <see langword="null"/> when
        /// the member is one element.
        /// </summary>
        internal List<XsdAlternative>? Choices { get; set; }

        /// <summary>
        /// Gets or sets the C# expression the property starts with, or an empty string for none.
        /// </summary>
        internal string Initializer { get; set; } = "";

        /// <summary>
        /// Gets or sets a value indicating whether the member is named by the generator and not by the schema,
        /// as the member of a choice is. Its name gives way to the names the schema gives.
        /// </summary>
        internal bool HasGeneratedName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the member's element is in no namespace although the schema
        /// has a target namespace.
        /// </summary>
        internal bool IsUnqualified { get; set; }

        /// <summary>
        /// Gets or sets the namespace of the items of a wrapped collection: a URI, an empty string for no
        /// namespace, or <see langword="null"/> when they take the default namespace in scope.
        /// </summary>
        internal string? ItemNamespace { get; set; }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public bool Equals(XsdMemberDetails? other)
        {
            if (other is null
                || Initializer != other.Initializer
                || HasGeneratedName != other.HasGeneratedName
                || IsUnqualified != other.IsUnqualified
                || ItemNamespace != other.ItemNamespace
                || (Choices?.Count ?? -1) != (other.Choices?.Count ?? -1))
            {
                return false;
            }

            for (var index = 0; index < (Choices?.Count ?? 0); index++)
            {
                if (!Choices![index].Equals(other.Choices![index]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return Equals(obj as XsdMemberDetails);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Initializer.GetHashCode() ^ (Choices?.Count ?? 0);
        }

        #endregion

    }

}
