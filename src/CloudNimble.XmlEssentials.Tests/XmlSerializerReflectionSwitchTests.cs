using System;
using CloudNimble.Text.Xml;
using CloudNimble.Text.Xml.Serialization;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// <see cref="XmlSerializer.IsReflectionEnabledByDefault"/> decides only which resolver options fall back to.
    /// The class runs alone because the switch is process-wide.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class XmlSerializerReflectionSwitchTests
    {

        #region Public Methods

        [TestMethod]
        public void SwitchOff_OptionsNeedAResolver_AndAnExplicitOneStillWorks()
        {
            var original = XmlSerializer.IsReflectionEnabledByDefault;
            original.Should().BeTrue();
            try
            {
                XmlSerializer.IsReflectionEnabledByDefault = false;
                var options = new XmlSerializerOptions();
                options.TypeInfoResolver.Should().BeNull();
                FluentActions.Invoking(() => XmlSerializer.Serialize(42, options)).Should().ThrowExactly<InvalidOperationException>().Which.Message.Should().Be(
                    "Reflection-based XML serialization is disabled. Set TypeInfoResolver, or use an overload that takes an XmlTypeInfo or XmlSerializerContext.");

                var explicitDefault = new XmlSerializerOptions { OmitXmlDeclaration = true, TypeInfoResolver = DefaultXmlTypeInfoResolver.Instance };
                XmlSerializer.Serialize(42, explicitDefault).Should().Be("<Int32>42</Int32>");
                var custom = new XmlSerializerOptions { OmitXmlDeclaration = true, TypeInfoResolver = new PassThroughResolver() };
                XmlSerializer.Serialize(42, custom).Should().Be("<Int32>42</Int32>");
                XmlSerializer.Serialize(42, new PrimitiveXmlContext(new XmlSerializerOptions { OmitXmlDeclaration = true })).Should().Be("<Int32>42</Int32>");
            }
            finally
            {
                XmlSerializer.IsReflectionEnabledByDefault = original;
            }
        }

        #endregion

    }

}
