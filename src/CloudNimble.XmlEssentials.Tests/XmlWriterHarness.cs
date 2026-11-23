using System;
using System.Buffers;
using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Shared helpers for <see cref="Utf8XmlWriter"/> tests.
    /// </summary>
    internal static class XmlWriterHarness
    {

        #region Public Methods

        /// <summary>
        /// Runs <paramref name="write"/> and returns the UTF-8 document as a string.
        /// </summary>
        /// <param name="write">The write callback.</param>
        /// <param name="omitDeclaration">When true, omits the XML declaration.</param>
        /// <returns>The written XML.</returns>
        public static string ToXml(Action<Utf8XmlWriter> write, bool omitDeclaration = true)
        {
            return ToXml(write, new XmlWriterOptions { OmitXmlDeclaration = omitDeclaration });
        }

        /// <summary>
        /// Runs <paramref name="write"/> with <paramref name="options"/> and returns the UTF-8 document as a string.
        /// </summary>
        /// <param name="write">The write callback.</param>
        /// <param name="options">Writer options.</param>
        /// <returns>The written XML.</returns>
        public static string ToXml(Action<Utf8XmlWriter> write, XmlWriterOptions options)
        {
            return Encoding.UTF8.GetString(ToBytes(write, options));
        }

        /// <summary>
        /// Runs <paramref name="write"/> and returns the UTF-8 bytes.
        /// </summary>
        /// <param name="write">The write callback.</param>
        /// <param name="options">Writer options.</param>
        /// <returns>The written bytes.</returns>
        public static byte[] ToBytes(Action<Utf8XmlWriter> write, XmlWriterOptions options)
        {
            var buffer = new ArrayBufferWriter<byte>();
            using (var writer = new Utf8XmlWriter(buffer, options))
            {
                write(writer);
            }

            return buffer.WrittenSpan.ToArray();
        }

        /// <summary>
        /// Runs <paramref name="write"/> and returns the message of the <see cref="XmlException"/> it throws.
        /// </summary>
        /// <param name="write">The write callback.</param>
        /// <param name="options">Writer options, or null for omitted declaration.</param>
        /// <returns>
        /// The message, or null when the callback did not throw. Disposing the writer afterwards may throw too,
        /// for an element left open; that is not the call under test and is ignored.
        /// </returns>
        public static string? ThrownMessage(Action<Utf8XmlWriter> write, XmlWriterOptions? options = null)
        {
            var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), options ?? new XmlWriterOptions { OmitXmlDeclaration = true });
            string? message = null;
            try
            {
                write(writer);
            }
            catch (XmlException ex)
            {
                message = ex.Message;
            }

            try
            {
                writer.Dispose();
            }
            catch (XmlException)
            {
            }

            return message;
        }

        /// <summary>
        /// Asserts that <paramref name="write"/> throws <see cref="XmlException"/>.
        /// </summary>
        /// <param name="write">The write callback.</param>
        /// <param name="options">Writer options, or null for omitted declaration.</param>
        public static void MustThrow(Action<Utf8XmlWriter> write, XmlWriterOptions? options = null)
        {
            var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), options ?? new XmlWriterOptions { OmitXmlDeclaration = true });
            var threw = false;
            try
            {
                write(writer);
            }
            catch (XmlException)
            {
                threw = true;
            }

            try
            {
                writer.Dispose();
            }
            catch (XmlException)
            {
                // Disposing a writer with an element still open throws too. That is not the call under
                // test, so it must not be what makes the test pass.
            }

            threw.Should().BeTrue("the calls under test must throw by themselves, before the writer is disposed");
        }

        #endregion

    }

}
