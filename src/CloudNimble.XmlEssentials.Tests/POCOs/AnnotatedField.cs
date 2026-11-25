namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A type with a public field annotated non-nullable.
    /// </summary>
    public sealed class AnnotatedField
    {

        #region Fields

        /// <summary>
        /// A value annotated as never null.
        /// </summary>
        public string Label = "l";

        #endregion

    }

}
