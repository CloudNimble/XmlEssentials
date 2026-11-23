namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An abstract class that declares no derived types, so the serializer has nothing to create for it.
    /// </summary>
    public abstract class LonelyShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        public string? Label { get; set; }

        #endregion

    }

}
