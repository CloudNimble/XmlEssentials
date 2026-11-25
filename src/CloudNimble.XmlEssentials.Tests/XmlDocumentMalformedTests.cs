using System;
using System.Buffers;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Malformed and reject paths for <see cref="XmlDocument"/>.
    /// </summary>
    [TestClass]
    public class XmlDocumentMalformedTests
    {

        #region Public Methods

        [TestMethod]
        public void Parse_Unclosed_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<e>");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_Empty_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_MaxDocumentByteCount_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<e/>", new XmlDocumentOptions { MaxDocumentByteCount = 2 });
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void GetChild_Missing_Throws()
        {
            using var document = XmlDocument.Parse("<e/>");
            var threw = false;
            try
            {
                document.RootElement.GetChild("nope");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void GetInt32_Invalid_Throws()
        {
            using var document = XmlDocument.Parse("<e>x</e>");
            var threw = false;
            try
            {
                document.RootElement.GetInt32();
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void DefaultElement_Throws()
        {
            var threw = false;
            try
            {
                _ = default(XmlElement).LocalName;
            }
            catch (InvalidOperationException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_NullString_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse((string)null!);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_NullStream_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse((System.IO.Stream)null!);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_Dtd_Throws()
        {
            FluentActions.Invoking(() => XmlDocument.Parse("<!DOCTYPE e><e/>")).Should().ThrowExactly<XmlException>().WithMessage("DTD is not allowed.");
        }

        [TestMethod]
        public void Parse_MismatchedEndTag_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<e></x>");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_DuplicateAttribute_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<e a=\"1\" a=\"2\"/>");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_UndeclaredPrefix_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<p:e/>");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_TruncatedAttribute_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<e a=\"1>");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_UnclosedCData_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<e><![CDATA[x</e>");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_UnknownEntity_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<e>&nope;</e>");
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_MaxDepth_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<a><b><c/></b></a>", new XmlDocumentOptions { MaxDepth = 1 });
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_MaxNameByteCount_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<abcdef/>", new XmlDocumentOptions { MaxNameByteCount = 2 });
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Parse_CommentDisallow_Throws()
        {
            var threw = false;
            try
            {
                XmlDocument.Parse("<!--x--><e/>", new XmlDocumentOptions { CommentHandling = XmlCommentHandling.Disallow });
            }
            catch (XmlException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Dispose_ThenWriteTo_Throws()
        {
            var document = XmlDocument.Parse("<e/>");
            document.Dispose();
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            var threw = false;
            try
            {
                document.WriteTo(writer);
            }
            catch (ObjectDisposedException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        #endregion

    }

}
