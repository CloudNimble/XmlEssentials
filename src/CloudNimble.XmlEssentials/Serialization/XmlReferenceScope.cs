using System;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Holds the <see cref="XmlReferenceResolver"/> for the current serialize or deserialize call.
    /// </summary>
    /// <remarks>
    /// The resolver belongs to the thread. Every caller sets it, reads or writes one value without awaiting
    /// anything, and puts the previous one back, so it never has to follow an asynchronous flow to another
    /// thread, and a thread-static costs far less to read and to set than an <c>AsyncLocal</c>.
    /// </remarks>
    internal static class XmlReferenceScope
    {

        #region Fields

        [ThreadStatic]
        private static XmlReferenceResolver? _current;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the resolver for the current operation, or null.
        /// </summary>
        internal static XmlReferenceResolver? Current
        {
            get => _current;
            set => _current = value;
        }

        #endregion

    }

}
