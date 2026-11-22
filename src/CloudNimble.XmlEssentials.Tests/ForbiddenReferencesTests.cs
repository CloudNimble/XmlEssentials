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
            names.Should().NotContain(static n => n.StartsWith("System.Xml", StringComparison.Ordinal));
            names.Should().NotContain("System.Private.Xml");
        }

        #endregion

    }

}
