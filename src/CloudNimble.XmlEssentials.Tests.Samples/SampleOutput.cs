using System;
using System.IO;

namespace CloudNimble.XmlEssentials.Tests.Samples
{

    /// <summary>
    /// Runs a sample and returns what it printed.
    /// </summary>
    internal static class SampleOutput
    {

        #region Public Methods

        /// <summary>
        /// Runs <paramref name="sample"/> against a writer that ends lines with a line feed.
        /// </summary>
        /// <param name="sample">The sample's entry point.</param>
        /// <returns>Everything the sample printed, without the final line end.</returns>
        public static string Capture(Action<TextWriter> sample)
        {
            using var writer = new StringWriter { NewLine = "\n" };
            sample(writer);
            var text = writer.ToString();
            return text.EndsWith('\n') ? text[..^1] : text;
        }

        #endregion

    }

}
