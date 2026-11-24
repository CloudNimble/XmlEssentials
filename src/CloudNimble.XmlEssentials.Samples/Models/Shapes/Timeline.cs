using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Samples.Models.Shapes
{

    /// <summary>
    /// A list of events of several types, where the element name says which type an event is.
    /// </summary>
    public sealed class Timeline
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets the events, in the order they happen.
        /// </summary>
        [XmlElement<string>("note")]
        [XmlElement<int>("pause")]
        [XmlElement<TimelineMark>("mark")]
        public List<object> Events { get; set; } = [];

        #endregion

    }

}