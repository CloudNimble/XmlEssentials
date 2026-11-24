using System.Diagnostics.CodeAnalysis;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type whose members change what their type says about null with flow attributes.
    /// </summary>
    public sealed class AnnotatedFlow
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value that can be set to null and never reads as null.
        /// </summary>
        [AllowNull]
        public string Allowed { get; set; } = "a";

        /// <summary>
        /// Gets or sets a nullable value that never reads as null.
        /// </summary>
        [NotNull]
        public string? Certain { get; set; } = "c";

        /// <summary>
        /// Gets or sets a nullable value that cannot be set to null.
        /// </summary>
        [DisallowNull]
        public string? Disallowed { get; set; } = "d";

        /// <summary>
        /// Gets or sets a non-nullable value that may read as null.
        /// </summary>
        [MaybeNull]
        public string Possibly { get; set; } = "p";

        #endregion

    }

}
