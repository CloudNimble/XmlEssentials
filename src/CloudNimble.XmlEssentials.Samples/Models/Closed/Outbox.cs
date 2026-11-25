// A closed type needs C# 15 and .NET 11, so this type exists only in the net11.0 build of the samples.
#if NET11_0_OR_GREATER
using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Closed
{

    /// <summary>
    /// The notifications that are waiting to be sent.
    /// </summary>
    public sealed class Outbox
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the notifications.
        /// </summary>
        [XmlArray]
        public List<Notification> Messages { get; set; } = [];

        #endregion

    }

}
#endif
