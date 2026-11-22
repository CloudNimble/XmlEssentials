using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Reads a whole stream into an array rented from <see cref="ArrayPool{T}.Shared"/>, stopping as soon as the
    /// stream is longer than the caller allows.
    /// </summary>
    internal static class XmlStreamBuffer
    {

        #region Private Methods

        /// <summary>
        /// Makes room for the next read.
        /// </summary>
        /// <param name="rented">The buffer. Replaced by a larger one when it grows.</param>
        /// <param name="length">The number of bytes read so far.</param>
        /// <param name="cap">The largest document allowed, or 0 for no limit.</param>
        /// <returns>
        /// <see langword="false"/> when the buffer is full and already holds <paramref name="cap"/> bytes, so
        /// the only thing left to learn is whether the stream has ended.
        /// </returns>
        internal static bool EnsureRoom(ref byte[] rented, int length, int cap)
        {
            if (length < rented.Length)
            {
                return true;
            }

            if (cap > 0 && length >= cap)
            {
                return false;
            }

            var grown = ArrayPool<byte>.Shared.Rent(cap > 0 ? Math.Min(length * 2, cap) : length * 2);
            rented.AsSpan(0, length).CopyTo(grown);
            XmlDocument.ReturnBuffer(rented, length);
            rented = grown;
            return true;
        }

        /// <summary>
        /// Reads <paramref name="utf8Xml"/> to its end.
        /// </summary>
        /// <param name="utf8Xml">The stream.</param>
        /// <param name="cap">The largest document allowed, or 0 for no limit.</param>
        /// <param name="length">The number of bytes read.</param>
        /// <returns>The rented array. The caller returns it with <see cref="XmlDocument.ReturnBuffer"/>.</returns>
        /// <exception cref="XmlException">The stream holds more than <paramref name="cap"/> bytes.</exception>
        internal static byte[] ReadToEnd(Stream utf8Xml, int cap, out int length)
        {
            var rented = ArrayPool<byte>.Shared.Rent(XmlLimits.DefaultBufferSize);
            length = 0;
            try
            {
                while (EnsureRoom(ref rented, length, cap))
                {
                    var read = utf8Xml.Read(rented, length, rented.Length - length);
                    if (read == 0)
                    {
                        return rented;
                    }

                    length += read;
                    XmlDocument.ThrowIfDocumentTooLarge(length, cap);
                }

                XmlDocument.ThrowIfDocumentTooLarge(length + (utf8Xml.ReadByte() < 0 ? 0 : 1), cap);
                return rented;
            }
            catch
            {
                XmlDocument.ReturnBuffer(rented, length);
                throw;
            }
        }

        /// <summary>
        /// Reads <paramref name="utf8Xml"/> to its end without blocking.
        /// </summary>
        /// <param name="utf8Xml">The stream.</param>
        /// <param name="cap">The largest document allowed, or 0 for no limit.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The rented array and the number of bytes read. The caller returns the array with <see cref="XmlDocument.ReturnBuffer"/>.</returns>
        /// <exception cref="XmlException">The stream holds more than <paramref name="cap"/> bytes.</exception>
        internal static async ValueTask<ArraySegment<byte>> ReadToEndAsync(Stream utf8Xml, int cap, CancellationToken cancellationToken)
        {
            var rented = ArrayPool<byte>.Shared.Rent(XmlLimits.DefaultBufferSize);
            var length = 0;
            try
            {
                while (EnsureRoom(ref rented, length, cap))
                {
                    var read = await utf8Xml.ReadAsync(rented.AsMemory(length), cancellationToken).ConfigureAwait(false);
                    if (read == 0)
                    {
                        return new ArraySegment<byte>(rented, 0, length);
                    }

                    length += read;
                    XmlDocument.ThrowIfDocumentTooLarge(length, cap);
                }

                XmlDocument.ThrowIfDocumentTooLarge(length + await utf8Xml.ReadAsync(new byte[1], cancellationToken).ConfigureAwait(false), cap);
                return new ArraySegment<byte>(rented, 0, length);
            }
            catch
            {
                XmlDocument.ReturnBuffer(rented, length);
                throw;
            }
        }

        #endregion

    }

}
