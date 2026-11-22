using System;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Thrown when XML is not well-formed or a DoS limit is exceeded.
    /// </summary>
    /// <remarks>
    /// This type is not <c>System.Xml.XmlException</c> and does not wrap it. Location properties
    /// are optional; they are set when the reader knows the byte offset and line.
    /// </remarks>
    public sealed class XmlException : Exception
    {

        #region Public Properties

        /// <summary>
        /// Gets the 0-based byte offset in the UTF-8 document, if known.
        /// </summary>
        public long? BytePosition { get; }

        /// <summary>
        /// Gets the 1-based line number after end-of-line normalization, if known.
        /// </summary>
        public long? LineNumber { get; }

        /// <summary>
        /// Gets the 1-based UTF-8 byte offset in the current line, if known.
        /// </summary>
        public long? LinePosition { get; }

        /// <summary>
        /// Gets the document path such as <c>/Person/@id</c>, if known.
        /// </summary>
        public string? Path { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates an exception with a message and no location.
        /// </summary>
        /// <param name="message">The error message.</param>
        public XmlException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Creates an exception with a message and an inner exception.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner exception, or null.</param>
        public XmlException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an exception with parse location.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="bytePosition">0-based byte offset in the UTF-8 document, if known.</param>
        /// <param name="lineNumber">1-based line after end-of-line normalization, if known.</param>
        /// <param name="linePosition">1-based UTF-8 byte offset in the current line, if known.</param>
        /// <param name="path">Document path such as <c>/Person/@id</c>, if known.</param>
        public XmlException(string message, long? bytePosition, long? lineNumber, long? linePosition, string? path)
            : base(message)
        {
            BytePosition = bytePosition;
            LineNumber = lineNumber;
            LinePosition = linePosition;
            Path = path;
        }

        #endregion

    }

}
