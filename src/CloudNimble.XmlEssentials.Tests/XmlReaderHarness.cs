using System.Text;
using CloudNimble.Text.Xml;
using FluentAssertions;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Shared drain helpers for well-formed and malformed reader tests.
    /// </summary>
    internal static class XmlReaderHarness
    {

        #region Public Methods

        public static void Drain(string xml, XmlReaderOptions options = default)
        {
            Drain(Encoding.UTF8.GetBytes(xml), options);
        }

        public static void Drain(byte[] xml, XmlReaderOptions options = default)
        {
            var reader = new Utf8XmlReader(xml, options);
            while (reader.Read())
            {
            }
        }

        public static void MustRead(string xml, XmlReaderOptions options = default)
        {
            Drain(xml, options);
        }

        public static void MustRead(byte[] xml, XmlReaderOptions options = default)
        {
            Drain(xml, options);
        }

        public static void MustThrow(string xml, XmlReaderOptions options = default)
        {
            var threw = false;
            try
            {
                Drain(xml, options);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue("expected XmlException for {0}", xml);
        }

        public static void MustThrow(byte[] xml, XmlReaderOptions options = default)
        {
            var threw = false;
            try
            {
                Drain(xml, options);
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue("expected XmlException for a binary payload of {0} bytes", xml.Length);
        }

        #endregion

    }

}
