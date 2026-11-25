using System;
using System.Buffers;
using System.IO;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// A <see cref="Utf8XmlWriter"/> borrowed from <see cref="XmlWriterCache"/> for one synchronous write.
    /// Disposing gives it back and drops anything the writer had not yet passed on. A document that fits the
    /// writer buffer therefore never reaches the destination half written; a longer one may have been passed on in part.
    /// </summary>
    internal readonly ref struct XmlWriterLease : IDisposable
    {

        #region Fields

        private readonly XmlPooledBufferWriter? _buffer;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the writer.
        /// </summary>
        internal Utf8XmlWriter Writer { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Borrows a writer that fills a pooled buffer. Read the result with <see cref="Flush"/>.
        /// </summary>
        /// <param name="options">The writer options.</param>
        /// <param name="capacity">The size of the first array, or 0 for the default.</param>
        internal XmlWriterLease(in XmlWriterOptions options, int capacity)
        {
            _buffer = XmlWriterCache.RentBuffer(capacity);
            Writer = XmlWriterCache.Rent(in options, _buffer);
        }

        /// <summary>
        /// Borrows a writer over <paramref name="bufferWriter"/>.
        /// </summary>
        /// <param name="options">The writer options.</param>
        /// <param name="bufferWriter">The destination.</param>
        internal XmlWriterLease(in XmlWriterOptions options, IBufferWriter<byte> bufferWriter)
        {
            Writer = XmlWriterCache.Rent(in options, bufferWriter);
        }

        /// <summary>
        /// Borrows a writer over <paramref name="utf8Xml"/>.
        /// </summary>
        /// <param name="options">The writer options.</param>
        /// <param name="utf8Xml">The destination.</param>
        internal XmlWriterLease(in XmlWriterOptions options, Stream utf8Xml)
        {
            Writer = XmlWriterCache.Rent(in options, utf8Xml);
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public void Dispose()
        {
            XmlWriterCache.Return(Writer, _buffer);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Flushes the writer and returns what the pooled buffer holds. Valid until <see cref="Dispose"/>.
        /// </summary>
        /// <returns>The document written so far.</returns>
        internal ReadOnlySpan<byte> Flush()
        {
            Writer.Flush();
            return _buffer!.WrittenSpan;
        }

        #endregion

    }

}
