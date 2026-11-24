using System;
using CloudNimble.Text.Xml;
using CloudNimble.XmlEssentials.Tests.POCOs;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Checks inside the serializer that no document or type reaches: each guards a precondition the callers keep.
    /// </summary>
    [TestClass]
    public class XmlSerializerInternalsTests
    {

        #region Public Methods

        [TestMethod]
        public void Populate_OnAConverterThatCannotPopulate_IsRejected()
        {
            // The binder asks CanPopulate before a member is populated, so only a caller that skips the question gets here.
            var options = new XmlSerializerOptions();
            var converter = options.GetConverter(typeof(int));
            converter.CanPopulate.Should().BeFalse();
            options.GetConverter(typeof(PopulateChild)).CanPopulate.Should().BeTrue();

            var threw = false;
            var reader = new Utf8XmlReader("<Int32>1</Int32>"u8);
            reader.Read();
            try
            {
                converter.Populate(ref reader, 1, options);
            }
            catch (NotSupportedException ex)
            {
                threw = true;
                ex.Message.Should().Be("The converter '" + converter.GetType() + "' cannot populate an existing value.");
            }

            threw.Should().BeTrue();
        }

        #endregion

    }

}
