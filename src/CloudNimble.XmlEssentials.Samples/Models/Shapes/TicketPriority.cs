namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// Specifies how urgent a ticket is.
    /// </summary>
    public enum TicketPriority
    {

        /// <summary>
        /// The ticket can wait.
        /// </summary>
        Low = 0,

        /// <summary>
        /// The ticket is handled in the normal queue.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// The ticket is handled first.
        /// </summary>
        High = 2,

    }

}
