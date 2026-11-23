using System.Collections.Generic;

namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An object that holds a record and a list of records, for tests that send a whole graph through one call.
    /// </summary>
    public sealed class SampleOrder
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        public string? Id { get; set; }

        /// <summary>
        /// Gets or sets the customer.
        /// </summary>
        public PersonRecord? Customer { get; set; }

        /// <summary>
        /// Gets or sets the lines.
        /// </summary>
        public List<SampleRecord> Lines { get; set; } = [];

        #endregion

    }

}
