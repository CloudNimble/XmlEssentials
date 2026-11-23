namespace CloudNimble.XmlEssentials.Samples.Models.Serialization
{

    /// <summary>
    /// Connection settings with init-only properties.
    /// </summary>
    public sealed class ConnectionSettings
    {

        #region Public Properties

        /// <summary>
        /// Gets or initializes the host name.
        /// </summary>
        public string? Host { get; init; }

        /// <summary>
        /// Gets or initializes the port. The default is 5432.
        /// </summary>
        public int Port { get; init; } = 5432;

        #endregion

    }

}
