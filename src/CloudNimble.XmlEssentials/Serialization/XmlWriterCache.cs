using System;
using System.Buffers;
using System.IO;

namespace CloudNimble.Text.Xml.Serialization
{

    /// <summary>
    /// Keeps one <see cref="Utf8XmlWriter"/> and one <see cref="XmlPooledBufferWriter"/> per thread for
    /// <see cref="XmlSerializer"/>, so a call that does not cross an <see langword="await"/> allocates neither.
    /// Use it through <see cref="XmlWriterLease"/>.
    /// </summary>
    /// <remarks>
    /// A converter may call <see cref="XmlSerializer"/> again while the outer call still holds the cached pair.
    /// The nested call gets new objects, and only the outermost call puts its pair back.
    /// </remarks>
    internal static class XmlWriterCache
    {

        #region Fields

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
            writer.Reset(bufferWriter);
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
            writer.Reset(utf8Xml);
            return writer;
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
            }
        }

        #endregion

    }

}
