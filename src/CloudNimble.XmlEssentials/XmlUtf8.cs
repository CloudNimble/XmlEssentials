using System;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Interned UTF-8 forms of well-known namespace URIs.
    /// </summary>
    internal static class XmlUtf8
    {

        #region Fields

        /// <summary>
        /// Empty URI (no namespace / unbound default).
        /// </summary>
        public static readonly byte[] Empty = [];

        /// <summary>
        /// UTF-8 of <see cref="XmlNamespaces.Xml"/>.
        /// </summary>
        public static readonly byte[] Xml = "http://www.w3.org/XML/1998/namespace"u8.ToArray();

        /// <summary>
        /// UTF-8 of <see cref="XmlNamespaces.Xmlns"/>.
        /// </summary>
        public static readonly byte[] Xmlns = "http://www.w3.org/2000/xmlns/"u8.ToArray();

        /// <summary>
        /// UTF-8 of <see cref="XmlNamespaces.Xsi"/>.
        /// </summary>
        public static readonly byte[] Xsi = "http://www.w3.org/2001/XMLSchema-instance"u8.ToArray();

        /// <summary>
        /// The reserved <c>xml</c> prefix.
        /// </summary>
        public static readonly byte[] XmlPrefix = "xml"u8.ToArray();

        /// <summary>
        /// The reserved <c>xmlns</c> prefix.
        /// </summary>
        public static readonly byte[] XmlnsPrefix = "xmlns"u8.ToArray();

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns an array that holds <paramref name="utf8"/> and does not depend on the buffer it is in:
        /// a shared array for nothing and for a well-known namespace, and otherwise a copy.
        /// </summary>
        /// <param name="utf8">The bytes, which may be in a buffer that is about to be replaced.</param>
        /// <returns>The array.</returns>
        public static byte[] Owned(ReadOnlySpan<byte> utf8)
        {
            return TryInternKnown(utf8) ?? utf8.ToArray();
        }

        /// <summary>
        /// Returns a well-known interned URI array when <paramref name="uri"/> matches, otherwise null.
        /// </summary>
        /// <param name="uri">The URI bytes.</param>
        /// <returns>An interned array, or null if the URI is not well-known.</returns>
        public static byte[]? TryInternKnown(ReadOnlySpan<byte> uri)
        {
            if (uri.IsEmpty)
            {
                return Empty;
            }

            if (uri.SequenceEqual(Xml))
            {
                return Xml;
            }

            if (uri.SequenceEqual(Xmlns))
            {
                return Xmlns;
            }

            if (uri.SequenceEqual(Xsi))
            {
                return Xsi;
            }

            return null;
        }

        #endregion

    }

}
