using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Paths
{

    /// <summary>
    /// A value that <see cref="PathOddConverter"/> reads and writes.
    /// </summary>
    [XmlConverter<PathOddConverter>]
    public sealed class PathOdd
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the path the converter gives the exception it throws when the value is written, or null to give none.
        /// </summary>
        public string? GivenPath { get; set; }

        #endregion

    }

}
