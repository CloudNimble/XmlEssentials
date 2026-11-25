namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a readonly field.
    /// </summary>
    public sealed class ReadOnlyFieldBox
    {

        #region Fields

        /// <summary>
        /// A field that is omitted when <see cref="CloudNimble.Text.Xml.XmlSerializerOptions.IgnoreReadOnlyFields"/> is true.
        /// </summary>
        public readonly string Value = "x";

        #endregion

    }

}
