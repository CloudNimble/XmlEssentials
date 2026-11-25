using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests
{

    /// <summary>
    /// Guards the runtime assembly against BCL XML references.
    /// </summary>
    [TestClass]
    public class ForbiddenReferencesTests
    {

        #region Public Methods

        /// <summary>
        /// The runtime assembly must not reference System.Xml* or System.Private.Xml.
        /// </summary>
        [TestMethod]
        public void RuntimeAssembly_DoesNotReferenceBclXml()
        {
            var path = Path.Combine(AppContext.BaseDirectory, "CloudNimble.XmlEssentials.dll");
            File.Exists(path).Should().BeTrue(because: path);

            var runtime = Assembly.LoadFrom(path);
            runtime.GetName().Name.Should().Be("CloudNimble.XmlEssentials");

            var names = runtime.GetReferencedAssemblies().Select(static a => a.Name!);
            names.Where(static n => n.StartsWith("System.Xml", StringComparison.Ordinal) || n is "System.Private.Xml").Should().BeEmpty();
        }

        /// <summary>
        /// The unit tests must not use BCL XML either: an expected value parsed by it would hide a defect both share.
        /// </summary>
        [TestMethod]
        public void TestAssembly_DoesNotReferenceBclXml()
        {
            var names = typeof(ForbiddenReferencesTests).Assembly.GetReferencedAssemblies().Select(static a => a.Name!);
            names.Where(static n => n.StartsWith("System.Xml", StringComparison.Ordinal) || n is "System.Private.Xml").Should().BeEmpty();
        }

        #endregion

    }

}
