using System;
using System.Collections.Generic;
using CloudNimble.Text.Xml.Serialization;

namespace CloudNimble.XmlEssentials.Tests.POCOs.Bcl
{

    /// <summary>
    /// Characters and points in time, as an attribute, an element and items of a collection.
    /// </summary>
    public class TypedValues
    {

        #region Public Properties

        /// <summary>
        /// Gets or sets a character written as an attribute.
        /// </summary>
        [XmlAttribute]
        public char Mark { get; set; }

        /// <summary>
        /// Gets or sets a point in time written as an attribute.
        /// </summary>
        [XmlAttribute]
        public DateTimeOffset Stamp { get; set; }

        /// <summary>
        /// Gets or sets a point in time with an offset that is not zero.
        /// </summary>
        public DateTimeOffset Away { get; set; }

        /// <summary>
        /// Gets or sets a character.
        /// </summary>
        public char Letter { get; set; }

        /// <summary>
        /// Gets or sets characters.
        /// </summary>
        [XmlArray]
        public List<char> Letters { get; set; } = [];

        /// <summary>
        /// Gets or sets a character that may be missing.
        /// </summary>
        public char? Maybe { get; set; }

        /// <summary>
        /// Gets or sets a point in time with a zero offset.
        /// </summary>
        public DateTimeOffset Sent { get; set; }

        /// <summary>
        /// Gets or sets points in time.
        /// </summary>
        [XmlArray]
        public List<DateTimeOffset> Times { get; set; } = [];

        /// <summary>
        /// Gets or sets a UTC <see cref="DateTime"/>, which the setting for offsets does not change.
        /// </summary>
        public DateTime When { get; set; }

        #endregion

    }

}
