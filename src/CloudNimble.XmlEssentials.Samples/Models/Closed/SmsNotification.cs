// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the samples.
#if NET11_0_OR_GREATER
namespace CloudNimble.XmlEssentials.Samples.Models.Closed
{

    /// <summary>
    /// A notification sent as a text message.
    /// </summary>
    public sealed class SmsNotification : Notification
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the phone number the notification is sent to.
        /// </summary>
        public string? Number { get; set; }

        #endregion

    }

}
#endif
