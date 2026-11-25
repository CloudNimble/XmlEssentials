// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the samples.
#if NET11_0_OR_GREATER
namespace CloudNimble.XmlEssentials.Samples.Models.Closed
{

    /// <summary>
    /// A notification sent by email.
    /// </summary>
    public sealed class EmailNotification : Notification
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the address the notification is sent to.
        /// </summary>
        public string? Address { get; set; }

        #endregion

    }

}
#endif
