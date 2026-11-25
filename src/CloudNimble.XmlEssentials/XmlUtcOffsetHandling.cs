namespace CloudNimble.Text.Xml
{

    /// <summary>
    /// Specifies how a <see cref="System.DateTimeOffset"/> whose offset from UTC is zero is written.
    /// </summary>
    /// <remarks>
    /// Both forms are valid ISO 8601 and <c>xs:dateTime</c> values for the same instant, and both are read under
    /// either value.
    /// </remarks>
    public enum XmlUtcOffsetHandling : byte
    {

        /// <summary>
        /// The offset in numbers, such as <c>2026-03-14T09:30:00+00:00</c>, as System.Text.Json writes it. This is
        /// the default.
        /// </summary>
        WriteOffset = 0,

        /// <summary>
        /// The ISO 8601 UTC designator <c>Z</c>, such as <c>2026-03-14T09:30:00Z</c>. This is the canonical form in XML Schema and
        /// what <c>System.Xml.Serialization.XmlSerializer</c> writes.
        /// </summary>
        WriteZ = 1,

    }

}
