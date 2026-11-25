// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the samples.
#if NET11_0_OR_GREATER
namespace CloudNimble.XmlEssentials.Samples.Models.Closed
{

    /// <summary>
    /// A notification. The type is closed: every type that derives from it is in this assembly.
    /// </summary>
    public closed class Notification
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the text of the notification.
        /// </summary>
        public string? Text { get; set; }

        #endregion

    }

}
#endif
