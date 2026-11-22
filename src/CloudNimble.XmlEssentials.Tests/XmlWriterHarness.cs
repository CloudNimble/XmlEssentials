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
        /// Asserts that <paramref name="write"/> throws <see cref="XmlException"/>.
        /// </summary>
        /// <param name="write">The write callback.</param>
        /// <param name="options">Writer options, or null for omitted declaration.</param>
        public static void MustThrow(Action<Utf8XmlWriter> write, XmlWriterOptions? options = null)
        {
            var threw = false;
            try
            {
                ToXml(write, options ?? new XmlWriterOptions { OmitXmlDeclaration = true });
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        #endregion

    }

}
