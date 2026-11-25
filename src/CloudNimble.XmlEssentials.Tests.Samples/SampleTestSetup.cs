using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CloudNimble.XmlEssentials.Tests.Samples
{

    /// <summary>
    /// Runs once before the sample tests.
    /// </summary>
    [TestClass]
    public static class SampleTestSetup
    {

        #region Public Methods

        /// <summary>
        /// Makes every test thread use the invariant culture.
        /// </summary>
        /// <param name="context">The test context.</param>
        /// <remarks>
        /// The samples print numbers and dates the way a reader's own program would, with the current culture.
        /// The documentation shows that output for the invariant culture, so the tests run under it and give the
        /// same result on every machine.
        /// </remarks>
        [AssemblyInitialize]
        public static void UseInvariantCulture(TestContext context)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        }

        #endregion

    }

}
