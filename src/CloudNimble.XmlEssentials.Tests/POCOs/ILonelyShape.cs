namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// An interface that declares no derived types, so the serializer has nothing to create for it.
    /// </summary>
    public interface ILonelyShape
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        string? Label { get; set; }

        #endregion

    }

}
