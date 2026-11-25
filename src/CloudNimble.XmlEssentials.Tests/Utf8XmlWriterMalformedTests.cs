using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;
using CloudNimble.Text.Xml;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Malformed and reject-path coverage for <see cref="Utf8XmlWriter"/>.
    /// </summary>
    [TestClass]
    public class Utf8XmlWriterMalformedTests
    {

        #region Public Methods

        [TestMethod]
        public void Write_EmptyName_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteStartElement(""));
        }

        [TestMethod]
        public void WriteEmptyElement_InvalidName_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteEmptyElement(""));
            XmlWriterHarness.MustThrow(w => w.WriteEmptyElement("1e"));
            XmlWriterHarness.MustThrow(w => w.WriteEmptyElement("a:b"));
            XmlWriterHarness.MustThrow(w => w.WriteEmptyElement("p", "e", null));
        }

        [TestMethod]
        public void Write_NameStartsWithDigit_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("1e"));
        }

        [TestMethod]
        public void Write_NameContainsColon_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("a:b"));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("a:b"u8));
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a:b");
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("");
            });
        }

        [TestMethod]
        public void Write_NameTooLong_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteStartElement(new string('a', 16)), new XmlWriterOptions { OmitXmlDeclaration = true, MaxNameByteCount = 8 });
        }

        [TestMethod]
        public void Write_PrefixedWithoutNamespace_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("p", "e", null));
        }

        [TestMethod]
        public void Write_NumberAtDocumentLevel_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteNumberValue(1));
        }

        [TestMethod]
        public void Write_EndWithNoStart_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteEndElement());
        }

        [TestMethod]
        public void Write_FullEndWithNoStart_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteFullEndElement());
        }

        [TestMethod]
        public void Write_MultipleRoots_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("a");
                w.WriteEndElement();
                w.WriteStartElement("b");
            });
        }

        [TestMethod]
        public void Write_TextAtDocumentLevel_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteStringValue("x"));
        }

        [TestMethod]
        public void Write_CDataAtDocumentLevel_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteCData("x"));
        }

        [TestMethod]
        public void Write_CDataCloser_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteCData("a]]>b");
            });
        }

        [TestMethod]
        public void Write_CommentDoubleHyphen_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteComment("a--b"));
        }

        [TestMethod]
        public void Write_CommentTrailingHyphen_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteComment("x-"));
        }

        [TestMethod]
        public void Write_PiTargetXml_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteProcessingInstruction("xml", "version='1.0'"));
            XmlWriterHarness.MustThrow(w => w.WriteProcessingInstruction("XML", "x"));
        }

        [TestMethod]
        public void Write_PiDataContainsCloser_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteProcessingInstruction("pi", "a?>b"));
        }

        [TestMethod]
        public void Write_AttributeWithoutElement_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteAttributeString("a", "1"));
        }

        [TestMethod]
        public void Write_AttributeAfterContent_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue("x");
                w.WriteAttributeString("a", "1");
            });
        }

        [TestMethod]
        public void Write_EndAttributeWithoutStart_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteEndAttribute();
            });
        }

        [TestMethod]
        public void Write_NilAfterContent_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue("x");
                w.WriteNilValue();
            });
        }

        [TestMethod]
        public void Write_NilAtDocumentLevel_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteNilValue());
        }

        [TestMethod]
        public void Write_PrefixUndeclareXml10_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteNamespaceDeclaration("p", "");
            });
        }

        [TestMethod]
        public void Write_RebindXmlPrefix_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteNamespaceDeclaration("xml", "urn:x");
            });
        }

        [TestMethod]
        public void Write_DeclareXmlnsPrefix_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteNamespaceDeclaration("xmlns", "urn:x");
            });
        }

        [TestMethod]
        public void Write_MaxDepth_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("a");
                w.WriteStartElement("b");
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxDepth = 1 });
        }

        [TestMethod]
        public void Write_MaxAttributeCount_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("a", "1");
                w.WriteAttributeString("b", "2");
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxAttributeCount = 1 });
        }

        [TestMethod]
        public void Write_IllegalCharInText_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue([(byte)0x01]);
            });
        }

        [TestMethod]
        public void Write_NulInText_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue([(byte)0]);
            });
        }

        [TestMethod]
        public void Write_InvalidUtf8InText_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue([(byte)0x80]);
            });
        }

        [TestMethod]
        public void Write_SecondDeclaration_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteXmlDeclaration();
                w.WriteXmlDeclaration();
            }, new XmlWriterOptions());
        }

        [TestMethod]
        public void Write_DeclarationAfterRoot_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteXmlDeclaration();
            }, new XmlWriterOptions { OmitXmlDeclaration = true });
        }

        [TestMethod]
        public void Write_UnclosedDispose_Throws()
        {
            var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            FluentActions.Invoking(writer.Dispose).Should().ThrowExactly<XmlException>().WithMessage("The document has unclosed elements.");
        }

        [TestMethod]
        public async Task Write_UnclosedDisposeAsync_Throws()
        {
            var writer = new Utf8XmlWriter(new ArrayBufferWriter<byte>(), new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            (await FluentActions.Awaiting(async () => await writer.DisposeAsync()).Should().ThrowExactlyAsync<XmlException>()).WithMessage("The document has unclosed elements.");
        }

        [TestMethod]
        public void Write_UseAfterDispose_Throws()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            writer.WriteEndElement();
            writer.Dispose();
            var threw = false;
            try
            {
                writer.WriteStartElement("e");
            }
            catch (ObjectDisposedException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Write_SurrogateInText_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue([(byte)0xED, 0xA0, 0x80]);
            });
        }

        [TestMethod]
        public void Write_Base64AtDocumentLevel_Throws()
        {
            XmlWriterHarness.MustThrow(w => w.WriteBase64StringValue("hi"u8));
        }

        [TestMethod]
        public void Ctor_NullBufferWriter_Throws()
        {
            var threw = false;
            try
            {
                _ = new Utf8XmlWriter((IBufferWriter<byte>)null!);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Ctor_NullStream_Throws()
        {
            var threw = false;
            try
            {
                _ = new Utf8XmlWriter((Stream)null!);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }

            threw.Should().BeTrue();
        }

        [TestMethod]
        public void Write_MalformedNames_Throw()
        {
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("-e"));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement(".e"));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("e e"));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("e$"));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("e\t"));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement(":e"));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("e:"));
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("p:e"));
            XmlWriterHarness.MustThrow(w => w.WriteStartAttribute(""));
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("1a");
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a:b");
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("-a");
            });
            XmlWriterHarness.MustThrow(w => w.WriteStartElement("e"u8[..0]));
            XmlWriterHarness.MustThrow(w => w.WriteProcessingInstruction("", "x"));
            XmlWriterHarness.MustThrow(w => w.WriteProcessingInstruction("1pi", "x"));
            XmlWriterHarness.MustThrow(w => w.WriteProcessingInstruction("Xml", "x"));
            XmlWriterHarness.MustThrow(w => w.WriteProcessingInstruction("xMl", "x"));
        }

        [TestMethod]
        public void Write_MalformedStructure_Throw()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("a");
                w.WriteEndElement();
                w.WriteEndElement();
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("a");
                w.WriteFullEndElement();
                w.WriteFullEndElement();
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteComment("c");
                w.WriteXmlDeclaration();
            }, new XmlWriterOptions());
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteProcessingInstruction("pi", "d");
                w.WriteXmlDeclaration();
            }, new XmlWriterOptions());
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue("x");
                w.WriteNamespaceDeclaration("p", "urn:x");
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue("x");
                w.WriteDefaultNamespace("urn:x");
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteStartAttribute("b");
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteCData("]]>");
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteCData("x]]>y");
            });
            XmlWriterHarness.MustThrow(w => w.WriteComment("--"));
            XmlWriterHarness.MustThrow(w => w.WriteComment("a--"));
            XmlWriterHarness.MustThrow(w => w.WriteComment("-"));
            XmlWriterHarness.MustThrow(w => w.WriteStringValueSegment("x"u8, true));
            XmlWriterHarness.MustThrow(w => w.WriteStringValueSegment("x".AsSpan(), true));
            XmlWriterHarness.MustThrow(w => w.WriteBase64StringSegment("hi"u8, true));
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("p", "a", null, "1");
            });
        }

        [TestMethod]
        public void Write_DuplicateAttribute_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("a", "1");
                w.WriteAttributeString("a", "2");
            });
        }

        [TestMethod]
        public void Write_DuplicateNamespacedAttribute_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("p", "a", "urn:x", "1");
                w.WriteAttributeString("p", "a", "urn:x", "2");
            });
        }

        [TestMethod]
        public void Write_DuplicateXmlns_Throws()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteNamespaceDeclaration("p", "urn:x");
                w.WriteNamespaceDeclaration("p", "urn:y");
            });
        }

        [TestMethod]
        public void Write_MalformedCharacters_Throw()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue([(byte)0x0B]);
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("a", "\u0001");
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue([(byte)0xEF, 0xBF, 0xBF]);
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute("a");
                w.WriteStringValue([(byte)0x00]);
            });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStringValue([(byte)0xC0, 0x80]);
            });
            XmlWriterHarness.MustThrow(w => w.WriteStartElement([(byte)0xFF]));
        }

        [TestMethod]
        public void Write_MalformedLimits_Throw()
        {
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteStartAttribute(new string('a', 16));
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxNameByteCount = 8 });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteNamespaceDeclaration(new string('p', 16), "urn:x");
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxNameByteCount = 8 });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("a");
                w.WriteStartElement("b");
                w.WriteStartElement("c");
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxDepth = 2 });
            XmlWriterHarness.MustThrow(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("a", "1");
                w.WriteAttributeString("b", "2");
                w.WriteAttributeString("c", "3");
            }, new XmlWriterOptions { OmitXmlDeclaration = true, MaxAttributeCount = 2 });
        }

        [TestMethod]
        public void Write_NullArguments_Throw()
        {
            var threw = 0;
            try
            {
                XmlWriterHarness.ToXml(w => w.WriteStartElement((string)null!));
            }
            catch (ArgumentNullException)
            {
                threw++;
            }

            try
            {
                XmlWriterHarness.ToXml(w => w.WriteEmptyElement((string)null!));
            }
            catch (ArgumentNullException)
            {
                threw++;
            }

            try
            {
                XmlWriterHarness.ToXml(w => w.WriteComment((string)null!));
            }
            catch (ArgumentNullException)
            {
                threw++;
            }

            try
            {
                XmlWriterHarness.ToXml(w => w.WriteCData((string)null!));
            }
            catch (ArgumentNullException)
            {
                threw++;
            }

            try
            {
                XmlWriterHarness.ToXml(w => w.WriteProcessingInstruction(null!, "x"));
            }
            catch (ArgumentNullException)
            {
                threw++;
            }

            try
            {
                XmlWriterHarness.ToXml(w => w.WriteRawValue((string)null!));
            }
            catch (ArgumentNullException)
            {
                threw++;
            }

            threw.Should().Be(6);
        }

        [TestMethod]
        public void Write_FlushAndResetAfterDispose_Throw()
        {
            var buffer = new ArrayBufferWriter<byte>();
            var writer = new Utf8XmlWriter(buffer, new XmlWriterOptions { OmitXmlDeclaration = true });
            writer.WriteStartElement("e");
            writer.WriteEndElement();
            writer.Dispose();
            var threw = 0;
            try
            {
                writer.Flush();
            }
            catch (ObjectDisposedException)
            {
                threw++;
            }

            try
            {
                writer.Reset();
            }
            catch (ObjectDisposedException)
            {
                threw++;
            }

            threw.Should().Be(2);
        }

        [TestMethod]
        public void Write_SameLocalDifferentNamespace_Allowed()
        {
            XmlWriterHarness.ToXml(w =>
            {
                w.WriteStartElement("e");
                w.WriteAttributeString("a", "1");
                w.WriteAttributeString("p", "a", "urn:x", "2");
                w.WriteEndElement();
            }).Should().Be("<e a=\"1\" xmlns:p=\"urn:x\" p:a=\"2\"/>");
        }

        #endregion

    }

}
