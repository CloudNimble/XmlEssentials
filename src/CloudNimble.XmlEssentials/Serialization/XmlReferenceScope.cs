using System.Threading;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Holds the <see cref="XmlReferenceResolver"/> for the current serialize or deserialize call.
    /// </summary>
    internal static class XmlReferenceScope
    {

        #region Fields

        private static readonly AsyncLocal<XmlReferenceResolver?> Resolver = new();

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the resolver for the current operation, or null.
        /// </summary>
        internal static XmlReferenceResolver? Current
        {
            get => Resolver.Value;
            set => Resolver.Value = value;
        }

        #endregion

    }

}
