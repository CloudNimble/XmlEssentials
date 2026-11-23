namespace CloudNimble.XmlEssentials.Tests.POCOs
{

    /// <summary>
    /// A generic box whose document element is named <c>PacketOf{T}</c>.
    /// </summary>
    /// <typeparam name="T">The payload type.</typeparam>
    public sealed class Packet<T>
    {

        #region Public Properties

        /// <summary>
        /// Gets the payload.
        /// </summary>
        public T? Value { get; set; }

        #endregion

    }

}
