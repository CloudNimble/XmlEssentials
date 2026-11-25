#nullable disable

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type compiled with nullable annotations off, so its members say nothing about null.
    /// </summary>
    public sealed class AnnotatedOblivious
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a value with no annotation.
        /// </summary>
        public string Name { get; set; } = "n";

        #endregion

    }

}
