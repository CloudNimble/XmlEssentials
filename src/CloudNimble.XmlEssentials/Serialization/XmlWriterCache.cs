using System;
using System.Buffers;
using System.IO;
using System.Threading;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Keeps one <see cref="Utf8XmlWriter"/> and one <see cref="XmlPooledBufferWriter"/> per thread for
    /// <see cref="XmlSerializer"/>, so a call that does not cross an <see langword="await"/> allocates neither.
    /// Use it through <see cref="XmlWriterLease"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A converter may call <see cref="XmlSerializer"/> again while the outer call still holds the cached pair.
    /// The nested call gets new objects, and only the outermost call puts its pair back.
    /// </para>
    /// <para>
    /// A call that does cross an <see langword="await"/> may go on on another thread, so it cannot use what a
    /// thread keeps. One spare writer is kept for those calls, for whichever of them asks first.
    /// </para>
    /// </remarks>
    internal static class XmlWriterCache
    {

        #region Fields

        /// <summary>
        /// The spare writer for calls that cross an <see langword="await"/>, or null while one of them has it.
        /// </summary>
        private static Utf8XmlWriter? s_spare;

        [ThreadStatic]
        private static XmlPooledBufferWriter? t_buffer;

        [ThreadStatic]
        private static int t_rented;

        [ThreadStatic]
        private static Utf8XmlWriter? t_writer;

        #endregion

        #region Private Methods

        /// <summary>
        /// Rents a writer over <paramref name="bufferWriter"/>.
        /// </summary>
        /// <param name="options">The writer options.</param>
        /// <param name="bufferWriter">The destination.</param>
        /// <returns>The writer.</returns>
        internal static Utf8XmlWriter Rent(in XmlWriterOptions options, IBufferWriter<byte> bufferWriter)
        {
            if (t_rented++ > 0 || t_writer is not { } writer)
            {
                return new Utf8XmlWriter(bufferWriter, options);
            }

            writer.Configure(in options);
            writer.Attach(bufferWriter);
            return writer;
        }

        /// <summary>
        /// Rents a writer over <paramref name="utf8Xml"/>.
        /// </summary>
        /// <param name="options">The writer options.</param>
        /// <param name="utf8Xml">The destination.</param>
        /// <returns>The writer.</returns>
        internal static Utf8XmlWriter Rent(in XmlWriterOptions options, Stream utf8Xml)
        {
            if (t_rented++ > 0 || t_writer is not { } writer)
            {
                return new Utf8XmlWriter(utf8Xml, options);
            }

            writer.Configure(in options);
            writer.Attach(utf8Xml);
            return writer;
        }

        /// <summary>
        /// Rents a writer over <paramref name="utf8Xml"/> for a call that crosses an <see langword="await"/>.
        /// </summary>
        /// <param name="options">The writer options.</param>
        /// <param name="utf8Xml">The destination.</param>
        /// <returns>The writer. It is given back with <see cref="ReturnSpare"/>.</returns>
        internal static Utf8XmlWriter RentSpare(in XmlWriterOptions options, Stream utf8Xml)
        {
            if (Interlocked.Exchange(ref s_spare, null) is not { } writer)
            {
                return new Utf8XmlWriter(utf8Xml, options);
            }

            writer.Configure(in options);
            writer.Attach(utf8Xml);
            return writer;
        }

        /// <summary>
        /// Gives back a writer from <see cref="RentSpare"/>, dropping anything it has not flushed.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <remarks>
        /// One writer is kept. When two calls were under way at once, the writer of the one that ends last
        /// is let go.
        /// </remarks>
        internal static void ReturnSpare(Utf8XmlWriter writer)
        {
            writer.Detach();
            if (Interlocked.CompareExchange(ref s_spare, writer, null) is not null)
            {
                writer.Dispose();
            }
        }

        /// <summary>
        /// Rents a pooled buffer for a writer to fill.
        /// </summary>
        /// <param name="capacity">The size of the first array, or 0 for the default.</param>
        /// <returns>The buffer.</returns>
        internal static XmlPooledBufferWriter RentBuffer(int capacity)
        {
            var buffer = t_rented == 0 && t_buffer is { } cached ? cached : new XmlPooledBufferWriter();
            buffer.Rent(XmlLimits.Resolve(capacity, XmlLimits.DefaultBufferSize));
            return buffer;
        }

        /// <summary>
        /// Gives back a writer, dropping anything it has not flushed, and the pooled buffer rented with it.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="buffer">The buffer from <see cref="RentBuffer"/>, or <see langword="null"/>.</param>
        internal static void Return(Utf8XmlWriter writer, XmlPooledBufferWriter? buffer)
        {
            writer.Detach();
            buffer?.Return();
            if (--t_rented == 0)
            {
                t_writer = writer;
                t_buffer = buffer ?? t_buffer;
                return;
            }

            // A writer that was made for a call inside another call is not kept, so it gives back what it holds.
            writer.Dispose();
        }

        #endregion

    }

}
