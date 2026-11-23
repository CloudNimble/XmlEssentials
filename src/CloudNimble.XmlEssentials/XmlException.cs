using System;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// The exception that is thrown when XML is not well formed, a reader limit is exceeded, or a value
    /// cannot be read as the requested type. This class cannot be inherited.
    /// </summary>
    /// <remarks>
    /// This type is not <c>System.Xml.XmlException</c> and does not wrap it. The location properties
    /// are optional. They are set when the reader knows the byte offset and line.
    /// </remarks>
    public sealed class XmlException : Exception
    {

        #region Public Properties

        /// <summary>
        /// Gets the zero-based byte offset in the UTF-8 document at which the error occurred.
        /// </summary>
        /// <value>
        /// The zero-based byte offset in the UTF-8 document, or <see langword="null"/> if it is not known.
        /// </value>
        public long? BytePosition { get; }

        /// <summary>
        /// Gets the one-based number of the line on which the error occurred.
        /// </summary>
        /// <value>
        /// The one-based line number, counted after end-of-line normalization, or <see langword="null"/> if
        /// it is not known.
        /// </value>
        public long? LineNumber { get; }

        /// <summary>
        /// Gets the one-based byte position within the line at which the error occurred.
        /// </summary>
        /// <value>
        /// The one-based UTF-8 byte offset in the line, or <see langword="null"/> if it is not known.
        /// </value>
        public long? LinePosition { get; }

        /// <summary>
        /// Gets the path in the document at which the error occurred.
        /// </summary>
        /// <value>
        /// The document path, such as <c>/Person/@id</c>, or <see langword="null"/> if it is not known.
        /// </value>
        public string? Path { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlException"/> class with a specified error message
        /// and no location.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public XmlException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or <see langword="null"/> if there is none.</param>
        public XmlException(string message, Exception? innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XmlException"/> class with a specified error message
        /// and the location of the error in the document.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="bytePosition">The zero-based byte offset in the UTF-8 document, or <see langword="null"/> if it is not known.</param>
        /// <param name="lineNumber">The one-based line number, counted after end-of-line normalization, or <see langword="null"/> if it is not known.</param>
        /// <param name="linePosition">The one-based UTF-8 byte offset in the line, or <see langword="null"/> if it is not known.</param>
        /// <param name="path">The document path, such as <c>/Person/@id</c>, or <see langword="null"/> if it is not known.</param>
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
