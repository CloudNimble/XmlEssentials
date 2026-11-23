namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A support ticket with an enumeration member.
    /// </summary>
    public sealed class Ticket
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the ticket number.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets how urgent the ticket is.
        /// </summary>
        public TicketPriority Priority { get; set; }

        #endregion

    }

}
