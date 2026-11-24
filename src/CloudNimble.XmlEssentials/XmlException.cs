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

        #region Fields

        /// <summary>
        /// Whether the first segment of the path is the name an element has in the document, which the caller
        /// that is one level out would otherwise give by the name of a member.
        /// </summary>
        private bool _elementNamed;

        /// <summary>
        /// Whether the library made the exception. The path is added to the message of such an exception, and
        /// not to the message of one that a caller made.
        /// </summary>
        private bool _appendPath;

        /// <summary>
        /// Whether the path was given to the constructor, and so is not to be changed.
        /// </summary>
        private readonly bool _pathGiven;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the message that describes the error.
        /// </summary>
        /// <value>
        /// The message. When the library made the exception and <see cref="Path"/> is set, the path follows the
        /// message, and the line and the position in the line follow the path when they are known:
        /// <c>The value is not a valid integer. Path: /Order/Lines[1]/Qty | LineNumber: 4 | BytePositionInLine: 50.</c>
        /// </value>
        /// <remarks>
        /// This is what <c>System.Text.Json</c> does with <c>JsonException.Message</c>. The message of an
        /// exception that your code makes, in a converter for example, is left as you wrote it.
        /// </remarks>
        public override string Message
        {
            get
            {
                if (!_appendPath || Path is null)
                {
                    return base.Message;
                }

                return LineNumber is null
                    ? $"{base.Message} Path: {Path}."
                    : string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{base.Message} Path: {Path} | LineNumber: {LineNumber} | BytePositionInLine: {LinePosition}.");
            }
        }

        /// <summary>
        /// Gets the zero-based byte offset in the UTF-8 document at which the error occurred.
        /// </summary>
        /// <value>
        /// The zero-based byte offset in the UTF-8 document, or <see langword="null"/> if it is not known.
        /// </value>
        public long? BytePosition { get; private set; }

        /// <summary>
        /// Gets the one-based number of the line on which the error occurred.
        /// </summary>
        /// <value>
        /// The one-based line number, counted after end-of-line normalization, or <see langword="null"/> if
        /// it is not known.
        /// </value>
        public long? LineNumber { get; private set; }

        /// <summary>
        /// Gets the one-based byte position within the line at which the error occurred.
        /// </summary>
        /// <value>
        /// The one-based UTF-8 byte offset in the line, or <see langword="null"/> if it is not known.
        /// </value>
        public long? LinePosition { get; private set; }

        /// <summary>
        /// Gets the path in the document at which the error occurred.
        /// </summary>
        /// <value>
        /// The document path, such as <c>/Order/Lines/Line[1]/@sku</c>, or <see langword="null"/> if it is not known.
        /// </value>
        /// <remarks>
        /// <para>
        /// <see cref="XmlSerializer"/> sets the path of an exception that is thrown while it reads or writes, as
        /// <c>System.Text.Json</c> sets <c>JsonException.Path</c>. The path starts at the document element and
        /// names each element on the way to the error. An attribute is <c>@name</c> and the text of an element
        /// is <c>text()</c>.
        /// </para>
        /// <para>
        /// When a document is read, an item of a collection has its position in the collection after its name,
        /// counted from 0 as <c>System.Text.Json</c> counts: <c>Line[1]</c> is the second item. When an object is written, the path is made of the
        /// elements the writer has open, and has no positions.
        /// </para>
        /// <para>
        /// A path that was given to the constructor is left as it is.
        /// </para>
        /// </remarks>
        public string? Path { get; private set; }

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
            _pathGiven = path is not null;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Makes an exception for an error the library found.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <returns>The exception. Callers throw it.</returns>
        internal static XmlException Create(string message)
        {
            return new XmlException(message) { _appendPath = true };
        }

        /// <summary>
        /// Makes an exception for an error the library found, with the exception that caused it.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that caused the error, or null.</param>
        /// <returns>The exception. Callers throw it.</returns>
        internal static XmlException Create(string message, Exception? innerException)
        {
            return new XmlException(message, innerException) { _appendPath = true };
        }

        /// <summary>
        /// Makes an exception for an error the library found at a known place in the document.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="bytePosition">The zero-based byte offset in the document, or null.</param>
        /// <param name="lineNumber">The one-based line number, or null.</param>
        /// <param name="linePosition">The one-based byte offset in the line, or null.</param>
        /// <param name="path">The document path, or null.</param>
        /// <returns>The exception. Callers throw it.</returns>
        internal static XmlException Create(string message, long? bytePosition, long? lineNumber, long? linePosition, string? path)
        {
            return new XmlException(message, bytePosition, lineNumber, linePosition, path) { _appendPath = true };
        }

        /// <summary>
        /// Puts an attribute before the path: the error was in the value of this attribute.
        /// </summary>
        /// <param name="localName">The local name of the attribute.</param>
        internal void AddAttribute(string localName)
        {
            Prepend("/@" + localName);
        }

        /// <summary>
        /// Puts an element before the path: the error was in this element.
        /// </summary>
        /// <param name="localName">The local name the element is expected to have.</param>
        /// <remarks>
        /// When the path already starts with the name the element has in the document, which a derived type
        /// gives it, that name stands and nothing is added.
        /// </remarks>
        internal void AddElement(string localName)
        {
            if (_elementNamed)
            {
                _elementNamed = false;
                return;
            }

            Prepend("/" + localName);
        }

        /// <summary>
        /// Puts an item of a collection before the path: the error was in this item.
        /// </summary>
        /// <param name="localName">The local name the item element is expected to have.</param>
        /// <param name="position">The position of the item in the collection, counted from 0 as an index is.</param>
        /// <remarks>
        /// When the path already starts with the name the item has in the document, which a derived type gives
        /// it, the position is put after that name.
        /// </remarks>
        internal void AddItem(string localName, int position)
        {
            if (_pathGiven)
            {
                return;
            }

            if (!_elementNamed)
            {
                Prepend("/" + localName + "[" + position.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]");
                return;
            }

            _elementNamed = false;
            var end = Path!.IndexOf('/', 1);
            var name = end < 0 ? Path : Path[..end];
            Path = name + "[" + position.ToString(System.Globalization.CultureInfo.InvariantCulture) + "]" + (end < 0 ? "" : Path[end..]);
        }

        /// <summary>
        /// Puts the text of an element before the path: the error was in the text.
        /// </summary>
        internal void AddText()
        {
            Prepend("/text()");
        }

        /// <summary>
        /// Puts an element before the path by the name it has in the document, so that the caller one level out
        /// does not name it again by the name of a member.
        /// </summary>
        /// <param name="localName">The local name of the element in the document.</param>
        internal void NameElement(string localName)
        {
            if (_pathGiven)
            {
                return;
            }

            Prepend("/" + localName);
            _elementNamed = true;
        }

        /// <summary>
        /// Gives the exception a path when it has none.
        /// </summary>
        /// <param name="path">The path.</param>
        internal void SetPathIfMissing(string path)
        {
            Path ??= path;
        }

        /// <summary>
        /// Gives the exception a position when it has none.
        /// </summary>
        /// <param name="bytePosition">The zero-based byte offset in the document.</param>
        /// <param name="lineNumber">The one-based line number.</param>
        /// <param name="linePosition">The one-based byte offset in the line.</param>
        internal void SetPositionIfMissing(long bytePosition, long lineNumber, long linePosition)
        {
            if (BytePosition is not null)
            {
                return;
            }

            BytePosition = bytePosition;
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }

        /// <summary>
        /// Puts a segment before the path, unless the path was given to the constructor.
        /// </summary>
        /// <param name="segment">The segment, with its leading slash.</param>
        private void Prepend(string segment)
        {
            if (!_pathGiven)
            {
                Path = segment + Path;
            }
        }

        #endregion
    }

}
